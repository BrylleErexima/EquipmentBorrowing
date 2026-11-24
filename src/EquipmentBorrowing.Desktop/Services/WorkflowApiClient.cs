using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EquipmentBorrowing.Contracts;

namespace EquipmentBorrowing.Desktop.Services;

public sealed class WorkflowApiClient(HttpClient http)
{
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Manila"));
    public Task<List<Student>> StudentsAsync() => Get<List<Student>>("students");
    public Task<List<Equipment>> EquipmentAsync() => Get<List<Equipment>>("equipment");
    public Task<List<Borrowing>> BorrowingsAsync(BorrowingQuery q)
    {
        if (q.StudentId is int student) return Get<List<Borrowing>>($"students/{student}/borrowings");
        if (q.EquipmentId is int item) return Get<List<Borrowing>>($"equipment/{item}/borrowings");
        return Get<List<Borrowing>>($"borrowings?search={Uri.EscapeDataString(q.Search)}&status={q.Status}&sort={(q.Sort == BorrowingSort.CheckoutDate ? "borrowedDate" : "expectedReturnDate")}&direction={(q.Descending ? "desc" : "asc")}");
    }
    public Task<ClientResult> SaveStudentAsync(int? id, string number, string name, string? contact) =>
        Send(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "students" : $"students/{id}", new StudentRequest(number, name, id is null && string.IsNullOrEmpty(contact) ? null : contact));
    public Task<ClientResult> SaveEquipmentAsync(int? id, string code, string name, string? description) =>
        Send(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "equipment" : $"equipment/{id}", new EquipmentRequest(code, name, description));
    public Task<ClientResult> SetEligibilityAsync(int id, bool allowed) => Send(HttpMethod.Patch, $"students/{id}/eligibility", new EligibilityRequest(allowed));
    public Task<ClientResult> CheckoutAsync(int student, int[] items, DateOnly? expected, bool multiple)
    {
        if (expected is null) return Task.FromResult(new ClientResult(false, "Expected return date is required."));
        if (!multiple && items.Length != 1) return Task.FromResult(new ClientResult(false, "Select one item."));
        return multiple ? Send(HttpMethod.Post, "checkout-requests", new CheckoutRequest(student, items, expected.Value))
            : Send(HttpMethod.Post, "borrowings", new BorrowingRequest(student, items[0], expected.Value));
    }
    public Task<ClientResult> ReturnAsync(int id) => Send(HttpMethod.Post, $"borrowings/{id}/return", new { });
    private async Task<T> Get<T>(string path)
    {
        try
        {
            using var response = await http.GetAsync(path);
            if (!response.IsSuccessStatusCode) throw new ApiClientException(await Feedback(response));
            return await response.Content.ReadFromJsonAsync<T>() ?? throw new ApiClientException("API returned an empty response. Refresh and retry.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        { throw new ApiClientException("API connection failed: no HTTP response was received. Check the API address and that it is running, then refresh."); }
        catch (JsonException) { throw new ApiClientException("API returned an invalid response. Check API compatibility and refresh."); }
    }
    private async Task<ClientResult> Send<T>(HttpMethod method, string path, T body)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
            using var response = await http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the published response, including single/multiple checkout results.
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var type = path == "checkout-requests" ? typeof(CheckoutResponse) : path.StartsWith("students") ? typeof(Student)
                    : path.StartsWith("equipment") ? typeof(Equipment) : typeof(Borrowing);
                if (json.RootElement.Deserialize(type, new JsonSerializerOptions(JsonSerializerDefaults.Web)) is null) throw new JsonException();
                return new(true, "Saved. Lists refreshed from the API.");
            }
            var error = await ReadError(response);
            return new(false, await Feedback(response, error), error?.Code == "outcome_unknown");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        { return new(false, "API connection failed: no complete HTTP response. Write outcome is unknown. Refresh successfully and inspect records before deciding to resubmit.", true); }
        catch (JsonException)
        { return new(false, "API response could not be read. Write outcome is unknown. Refresh and inspect records before deciding to resubmit.", true); }
    }
    private static async Task<ApiError?> ReadError(HttpResponseMessage response)
    {
        try { return await response.Content.ReadFromJsonAsync<ApiError>(); } catch (JsonException) { return null; }
    }
    private static async Task<string> Feedback(HttpResponseMessage response, ApiError? error = null)
    {
        error ??= await ReadError(response);
        var prefix = (int)response.StatusCode >= 500 ? $"API server error (HTTP {(int)response.StatusCode}). " : $"HTTP {(int)response.StatusCode}: ";
        return prefix + (error?.Message ?? "Request failed. Refresh and retry.");
    }
}
