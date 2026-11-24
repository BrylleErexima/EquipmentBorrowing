using System.Text.Json;
using System.Text.Json.Serialization;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using C = EquipmentBorrowing.Contracts;
using D = EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Api;

public static class ApiHost
{
    private static readonly JsonSerializerOptions Input = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        PropertyNameCaseInsensitive = false
    };

    // The acceptance runner can replace configuration/interceptors without exposing a fault endpoint.
    public static WebApplication Create(string[] args, Action<IServiceCollection>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContextFactory<EquipmentBorrowingDbContext>(o => o.UseNpgsql(PostgresConfiguration.ConnectionString));
        builder.Services.AddSingleton<IWorkflowRepository, PostgresWorkflowRepository>();
        builder.Services.AddSingleton<WorkflowService>();
        configure?.Invoke(builder.Services);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
                if (context.Response.StatusCode is 404 or 405 && !context.Response.HasStarted)
                    await context.Response.WriteAsJsonAsync(new C.ApiError(context.Response.StatusCode == 404 ? "not_found" : "method_not_allowed",
                        context.Response.StatusCode == 404 ? "Resource or route not found." : "HTTP method is not allowed for this route."));
            }
            catch (WorkflowException e) { await Error(context, e.StatusCode, Code(e.StatusCode), e.Message); }
            catch (Exception e) when (e is JsonException or BadHttpRequestException)
            { await Error(context, 400, "invalid_request", "Invalid JSON, missing required fields, incorrect types, or unknown/protected fields."); }
            catch (PersistenceException e) { await Error(context, e.StatusCode, e.OutcomeUnknown ? "outcome_unknown" : Code(e.StatusCode), e.Message); }
            catch (Exception)
            {
                // Never log connection strings, request bodies or exception details.
                app.Logger.LogError("Request failed: {Method} {Path}", context.Request.Method, context.Request.Path);
                await Error(context, 500, "server_error", "Server operation failed. Refresh and retry; contact the custodian if it persists.");
            }
        });
        app.MapGet("/openapi/v1.json", () => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.json"), "application/json"));
        var api = app.MapGroup("/api/v1");
        api.MapGet("/equipment", async (HttpContext c, WorkflowService s) =>
        {
            Query(c, "available");
            bool? available = null;
            if (c.Request.Query.TryGetValue("available", out var v))
            {
                if (!bool.TryParse(v, out var b)) throw new WorkflowException("available must be true or false.", 400);
                available = b;
            }
            return Results.Ok((await s.EquipmentAsync()).Where(x => available is null || x.IsAvailable == available).Select(Map));
        });
        api.MapGet("/students", async (HttpContext c, WorkflowService s) =>
        {
            Query(c, "search"); var search = c.Request.Query["search"].ToString().Trim();
            return Results.Ok((await s.StudentsAsync()).Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || x.StudentNumber.Contains(search, StringComparison.OrdinalIgnoreCase)).Select(Map));
        });
        api.MapGet("/students/{id}", async (string id, WorkflowService s) => Results.Ok(Map(await s.StudentAsync(Id(id)))));
        api.MapGet("/equipment/{id}", async (string id, WorkflowService s) => Results.Ok(Map(await s.ItemAsync(Id(id)))));
        api.MapPost("/students", async (HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.StudentRequest>(c); var result = await s.SaveStudentAsync(null, -1, r.StudentNumber, r.FullName, r.Contact);
            return StudentResult(result, true);
        });
        api.MapPut("/students/{id}", async (string id, HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.StudentUpdateRequest>(c); return StudentResult(await s.SaveStudentAsync(Id(id), -1, r.StudentNumber, r.FullName, r.Contact));
        });
        api.MapPatch("/students/{id}/eligibility", async (string id, HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.EligibilityRequest>(c); return StudentResult(await s.SetEligibilityAsync(Id(id), -1, r.IsEligible));
        });
        api.MapPost("/equipment", async (HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.EquipmentRequest>(c); return EquipmentResult(await s.SaveEquipmentAsync(null, -1, r.EquipmentCode, r.Name, r.Description), true);
        });
        api.MapPut("/equipment/{id}", async (string id, HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.EquipmentUpdateRequest>(c); return EquipmentResult(await s.SaveEquipmentAsync(Id(id), -1, r.EquipmentCode, r.Name, r.Description));
        });
        api.MapPost("/borrowings", async (HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.BorrowingRequest>(c);
            Positive(r.StudentId, r.EquipmentId);
            return CheckoutResult(await s.CheckoutAsync(r.StudentId, [r.EquipmentId], r.ExpectedReturnDate, false), false);
        });
        api.MapPost("/checkout-requests", async (HttpContext c, WorkflowService s) =>
        {
            var r = await Body<C.CheckoutRequest>(c);
            Positive(r.StudentId); if (r.EquipmentIds is null) throw new WorkflowException("equipmentIds is required.", 400);
            return CheckoutResult(await s.CheckoutAsync(r.StudentId, r.EquipmentIds, r.ExpectedReturnDate, true), true);
        });
        api.MapPost("/borrowings/{id}/return", async (string id, HttpContext c, WorkflowService s) =>
        {
            var r = await Body<JsonElement>(c);
            if (r.ValueKind != JsonValueKind.Object || r.EnumerateObject().Any()) throw new WorkflowException("Return requires an empty JSON object.", 400);
            var result = await s.ReturnAsync(Id(id));
            if (!result.Success) return Failure(result);
            return Results.Ok(Map((D.Borrowing)result.Value!));
        });
        api.MapGet("/borrowings/{id}", async (string id, WorkflowService s) => Results.Ok(Map(await s.BorrowingAsync(Id(id)))));
        api.MapGet("/borrowings", async (HttpContext c, WorkflowService s) =>
        {
            Query(c, "search", "status", "sort", "direction");
            var status = c.Request.Query["status"].ToString(); var sort = c.Request.Query["sort"].ToString(); var direction = c.Request.Query["direction"].ToString();
            var filter = status switch { "" or "All" => StatusFilter.All, "Active" => StatusFilter.Active, "Returned" => StatusFilter.Returned, _ => throw new WorkflowException("Unsupported status.", 400) };
            var order = sort switch { "" or "borrowedDate" => BorrowingSort.CheckoutDate, "expectedReturnDate" => BorrowingSort.ExpectedReturnDate, _ => throw new WorkflowException("Unsupported sort.", 400) };
            if (direction is not ("" or "asc" or "desc")) throw new WorkflowException("Unsupported direction.", 400);
            return Results.Ok((await s.BorrowingsAsync(new(c.Request.Query["search"].ToString(), filter, order, direction == "desc"))).Select(Map));
        });
        api.MapGet("/students/{id}/borrowings", async (string id, WorkflowService s) => Results.Ok((await s.BorrowingsAsync(new(StudentId: Id(id)))).Select(Map)));
        api.MapGet("/equipment/{id}/borrowings", async (string id, WorkflowService s) => Results.Ok((await s.BorrowingsAsync(new(EquipmentId: Id(id)))).Select(Map)));
        return app;
    }

    private static async Task<T> Body<T>(HttpContext c)
    {
        if (!c.Request.HasJsonContentType()) throw new WorkflowException("Content-Type must be application/json.", 400);
        return await JsonSerializer.DeserializeAsync<T>(c.Request.Body, Input) ?? throw new WorkflowException("JSON body is required.", 400);
    }
    private static int Id(string id) => int.TryParse(id, out var value) && value > 0 ? value : throw new WorkflowException("Resource not found.", 404);
    private static void Positive(params int[] ids) { if (ids.Any(x => x <= 0)) throw new WorkflowException("IDs must be positive integers.", 400); }
    private static void Query(HttpContext c, params string[] names)
    {
        if (c.Request.Query.Any(x => !names.Contains(x.Key) || x.Value.Count != 1)) throw new WorkflowException("Unsupported or repeated query parameter.", 400);
    }
    private static string Code(int status) => status switch { 400 => "invalid_request", 404 => "not_found", 409 => "conflict", _ => "server_error" };
    private static Task Error(HttpContext c, int status, string code, string message)
    {
        c.Response.StatusCode = status; return c.Response.WriteAsJsonAsync(new C.ApiError(code, message));
    }
    private static IResult Failure(OperationResult r) => Results.Json(new C.ApiError(r.OutcomeUnknown ? "outcome_unknown" : Code(r.StatusCode), r.Message), statusCode: r.StatusCode);
    private static IResult StudentResult(OperationResult r, bool created = false)
    {
        if (!r.Success) return Failure(r); var student = Map((D.Student)r.Value!);
        return created ? Results.Created($"/api/v1/students/{student.Id}", student) : Results.Ok(student);
    }
    private static IResult EquipmentResult(OperationResult r, bool created = false)
    {
        if (!r.Success) return Failure(r); var item = Map((D.Equipment)r.Value!);
        return created ? Results.Created($"/api/v1/equipment/{item.Id}", item) : Results.Ok(item);
    }
    private static IResult CheckoutResult(OperationResult r, bool multiple)
    {
        if (!r.Success) return Failure(r);
        var loans = new List<C.Borrowing>();
        foreach (var loan in (List<D.Borrowing>)r.Value!) loans.Add(Map(loan));
        return multiple ? Results.Json(new C.CheckoutResponse(loans), statusCode: 201) : Results.Created($"/api/v1/borrowings/{loans[0].Id}", loans[0]);
    }
    private static C.Student Map(D.Student x) => new(x.Id, x.StudentNumber, x.Name, x.Contact, x.CanBorrow);
    private static C.Equipment Map(D.Equipment x) => new(x.Id, x.Identifier, x.Name, x.Description, x.IsAvailable);
    private static C.Borrowing Map(D.Borrowing x) => new(x.Id, x.StudentId, x.EquipmentId, x.DateBorrowed, x.ExpectedReturnDate, x.ActualReturnDate, x.Status.ToString(), Map(x.Student), Map(x.Equipment));
}
