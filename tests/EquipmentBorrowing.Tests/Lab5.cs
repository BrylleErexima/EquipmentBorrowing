using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EquipmentBorrowing.Api;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using C = EquipmentBorrowing.Contracts;
using D = EquipmentBorrowing.Domain;

// A proportional executable of the supplied T01–T08 catalog, using real Kestrel HTTP and PostgreSQL.
// Fault injection exists only in this runner; the production API exposes no fault/reset routes.
static class Lab5
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static async Task<int> Run(string[] args)
    {
        var admin = Environment.GetEnvironmentVariable("EQUIPMENTBORROWING_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(admin))
        {
            Console.Error.WriteLine("Not run: set EQUIPMENTBORROWING_TEST_CONNECTION to a local login with CREATEDB. No credentials are printed.");
            return 2;
        }
        if (args.Length != 0 && (args.Length != 2 || args[0] != "--readback")) throw new ArgumentException("Use --lab5 or --lab5 --readback equipment_lab5_test_<guid>.");
        var database = args.Length == 0 ? "equipment_lab5_test_" + Guid.NewGuid().ToString("N") : args[1];
        if (!Regex.IsMatch(database, "^equipment_lab5_test_[a-f0-9]{32}$")) throw new ArgumentException("Unsafe database name.");
        if (args.Length == 0)
        {
            await using var connection = new NpgsqlConnection(admin); await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection); await command.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(admin) { Database = database }.ConnectionString;
        Console.WriteLine($"UTC {DateTimeOffset.UtcNow:O}; local Asia/Manila date {WorkflowService.Today:yyyy-MM-dd}; database {database}; .NET {Environment.Version}; OS {Environment.OSVersion}");
        var fault = new Faults(); var factory = new Factory(connectionString, fault);
        await using (var db = factory.CreateDbContext()) await db.Database.MigrateAsync();
        WebApplication? app = null;
        HttpClient? http = null;
        async Task Start(string? connection = null)
        {
            var f = connection is null ? factory : new Factory(connection, new Faults());
            app = ApiHost.Create(["--urls", "http://127.0.0.1:0", "--Logging:LogLevel:Default", "Warning"], services =>
                services.AddSingleton(new WorkflowService(new PostgresWorkflowRepository(f))));
            await app.StartAsync();
            http = new HttpClient(new CaptureHandler()) { BaseAddress = new Uri(app.Urls.Single() + "/"), Timeout = TimeSpan.FromSeconds(15) };
            Console.WriteLine("API listening " + http.BaseAddress + "; no Avalonia process required.");
        }
        async Task Stop()
        {
            if (app is not null) { await app.StopAsync(); await app.DisposeAsync(); app = null; }
        }
        async Task<JsonElement> Call(string method, string path, int expected = 200, object? body = null)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path.StartsWith('/') ? path : "/api/v1/" + path);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http!.SendAsync(request);
            Check((int)response.StatusCode == expected, $"{method} {path} expected {expected}, got {(int)response.StatusCode}");
            if (expected == 201 && path is "students" or "equipment" or "borrowings")
            {
                Check(response.Headers.Location is not null, "201 includes Location");
                using var readback = await http.GetAsync(response.Headers.Location);
                Check(readback.StatusCode == HttpStatusCode.OK, "Location resolves");
            }
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        }
        async Task<string> Snapshot(string label)
        {
            // An independent Npgsql connection, not the API's DbContext/change tracker.
            await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT json_build_object('students',(SELECT coalesce(json_agg(t ORDER BY t.\"Id\"),'[]'::json) FROM \"Students\" t),'equipment',(SELECT coalesce(json_agg(t ORDER BY t.\"Id\"),'[]'::json) FROM \"Equipments\" t),'borrowings',(SELECT coalesce(json_agg(t ORDER BY t.\"Id\"),'[]'::json) FROM \"Borrowings\" t))::text", connection);
            var snapshot = (string)(await command.ExecuteScalarAsync())!;
            Console.WriteLine($"DB {label}: {snapshot}"); return snapshot;
        }
        async Task<JsonElement> Mutation(string method, string path, object body, int expected)
        {
            var before = await Snapshot("before " + method + " " + path);
            var response = await Call(method, path, expected, body);
            var after = await Snapshot("after " + method + " " + path);
            if (expected >= 400) Check(before == after, "Rejected/known-failure mutation leaves fresh PostgreSQL state unchanged");
            return response;
        }
        Dictionary<string, int> students = new(), items = new();
        int S(int n) => students[$"ST-{n}00"];
        int E(int n) => items[$"EQ-{n:000}"];
        object Single(int s, int e, int days = 2) => new { studentId = s, equipmentId = e, expectedReturnDate = WorkflowService.Today.AddDays(days) };
        object Multiple(int s, params int[] equipment) => new { studentId = s, equipmentIds = equipment, expectedReturnDate = WorkflowService.Today.AddDays(2) };
        async Task Reset()
        {
            fault.AfterSave = fault.AfterCommit = false;
            // Only the newly created, validated disposable database is ever reset.
            await using var db = factory.CreateDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Borrowings.ExecuteDeleteAsync(); await db.Equipments.ExecuteDeleteAsync(); await db.Students.ExecuteDeleteAsync();
            var ss = new[] { new D.Student("ST-100", "Ada Cruz"), new D.Student("ST-200", "Ben Diaz"), new D.Student("ST-300", "Cara Lim"), new D.Student("ST-400", "Dani Ong") };
            ss[1].SetEligibility(false);
            var names = new[] { "Camera", "Laptop", "Projector", "Tablet", "Microphone", "Tripod", "Speaker" };
            var es = names.Select((name, i) => new D.Equipment($"EQ-{i + 1:000}", name)).ToArray();
            db.AddRange(ss); db.AddRange(es); await db.SaveChangesAsync();
            var today = WorkflowService.Today;
            var returned = new D.Borrowing(ss[0].Id, es[2].Id, today.AddDays(-8), today.AddDays(2)); returned.MarkReturned(today.AddDays(-2));
            db.AddRange(new D.Borrowing(ss[0].Id, es[0].Id, today.AddDays(-9), today.AddDays(-1)), returned,
                new D.Borrowing(ss[2].Id, es[3].Id, today.AddDays(-7), today.AddDays(2)), new D.Borrowing(ss[2].Id, es[4].Id, today.AddDays(-7), today.AddDays(2)));
            es[0].SetAvailability(false); es[3].SetAvailability(false); es[4].SetAvailability(false);
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            students = (await Call("GET", "students")).EnumerateArray().ToDictionary(x => x.GetProperty("studentNumber").GetString()!, x => x.GetProperty("id").GetInt32());
            items = (await Call("GET", "equipment")).EnumerateArray().ToDictionary(x => x.GetProperty("equipmentCode").GetString()!, x => x.GetProperty("id").GetInt32());
            Console.WriteLine("RESET controlled S1–S4/E1–E7 dataset; all request IDs resolved from HTTP responses.");
        }
        try
        {
            await Start();
            if (args.Length > 0)
            {
                await Snapshot("new process readback");
                var rows = await Call("GET", "students?search=UI-001"); Check(rows.GetArrayLength() == 1, "T08 separate process retains UI student");
                var equipment = await Call("GET", "equipment"); Check(equipment.EnumerateArray().Any(x => x.GetProperty("name").GetString() == "Independent HTTP edit"), "T08 separate process retains independent edit");
                Console.WriteLine("PASS T08 new-process persistence readback"); return 0;
            }
            await Reset();
            Console.WriteLine("BEGIN T01 independent reads and OpenAPI");
            var before = await Snapshot("T01 before");
            Check((await Call("GET", "equipment")).GetArrayLength() == 7, "seven items");
            var available = await Call("GET", "equipment?available=true");
            Check(available.GetArrayLength() == 4 && available.EnumerateArray().All(x => x.GetProperty("isAvailable").GetBoolean()), "four available items");
            Check((await Call("GET", "students")).GetArrayLength() == 4, "four students");
            await Call("GET", $"students/{S(1)}"); await Call("GET", $"equipment/{E(1)}");
            var history = await Call("GET", $"students/{S(1)}/borrowings");
            Check(history.GetArrayLength() == 2, "S1 active and returned");
            var initialLoan = history[0].GetProperty("id").GetInt32();
            await Call("GET", $"borrowings/{initialLoan}"); await Call("GET", $"equipment/{E(1)}/borrowings");
            Check((await Call("GET", "borrowings?status=Active")).GetArrayLength() == 3, "three active");
            Check((await Call("GET", $"students/{S(4)}/borrowings")).GetArrayLength() == 0, "empty history array");
            foreach (var path in new[] { "students/2147483647", "equipment/2147483647", "borrowings/2147483647", "wrong-route" }) await Call("GET", path, 404);
            await Call("DELETE", "equipment", 405);
            await Call("GET", "equipment?available=bad", 400); await Call("GET", "students?unsupported=x", 400);
            var openapi = await Call("GET", "/openapi/v1.json");
            var operations = openapi.GetProperty("paths").EnumerateObject().SelectMany(p => p.Value.EnumerateObject()).ToArray();
            Check(operations.Length == 16 && operations.Select(x => x.Value.GetProperty("operationId").GetString()).Distinct().Count() == 16, "sixteen distinct OpenAPI contracts");
            Check(before == await Snapshot("T01 after"), "T01 read-only"); Console.WriteLine("PASS T01");

            await Reset(); Console.WriteLine("BEGIN T02 student management");
            var created = await Mutation("POST", "students", new { studentNumber = "ST-500", fullName = "Eli Santos", contact = (string?)null }, 201);
            var newStudent = created.GetProperty("id").GetInt32(); Check(created.GetProperty("isEligible").GetBoolean(), "new student eligible");
            await Mutation("PUT", $"students/{newStudent}", new { studentNumber = "ST-500", fullName = "Eli Edited", contact = "local extension" }, 200);
            await Mutation("POST", "students", new { studentNumber = "ST-600", fullName = " " }, 400);
            await Mutation("POST", "students", new { studentNumber = " st-100 ", fullName = "Duplicate" }, 409);
            await Mutation("POST", "students", new { studentNumber = "ST-600", fullName = "Name", contact = " " }, 400);
            await Mutation("PATCH", $"students/{S(1)}/eligibility", new { isEligible = false }, 200);
            await Mutation("POST", "borrowings", Single(S(1), E(2)), 409);
            var active = (await Call("GET", $"equipment/{E(1)}/borrowings"))[0].GetProperty("id").GetInt32();
            await Mutation("POST", $"borrowings/{active}/return", new { }, 200);
            await Mutation("PATCH", $"students/{S(1)}/eligibility", new { isEligible = true }, 200);
            await Mutation("POST", "borrowings", Single(S(1), E(2)), 201);
            before = await Snapshot("same eligibility before"); await Mutation("PATCH", $"students/{S(1)}/eligibility", new { isEligible = true }, 200);
            Check(before == await Snapshot("same eligibility after"), "same eligibility is no-op");
            await Stop(); await Start(); Check((await Call("GET", $"students/{newStudent}")).GetProperty("fullName").GetString() == "Eli Edited", "T02 restart retains edit");
            Console.WriteLine("PASS T02");

            await Reset(); Console.WriteLine("BEGIN T03 equipment management");
            created = await Mutation("POST", "equipment", new { equipmentCode = "EQ-008", name = "Monitor", description = (string?)null }, 201);
            int newItem = created.GetProperty("id").GetInt32(); Check(created.GetProperty("isAvailable").GetBoolean(), "new item available");
            await Mutation("PUT", $"equipment/{newItem}", new { equipmentCode = "EQ-008", name = "Edited Monitor", description = " " }, 200);
            await Mutation("PUT", $"equipment/{E(1)}", new { equipmentCode = "EQ-001", name = "Edited Camera", description = (string?)null }, 200);
            Check(!(await Call("GET", $"equipment/{E(1)}")).GetProperty("isAvailable").GetBoolean(), "descriptive edit preserves unavailable state");
            Check((await Call("GET", $"equipment/{E(1)}/borrowings")).GetArrayLength() == 1, "E1 relationship preserved");
            await Mutation("POST", "equipment", new { equipmentCode = " eq-001 ", name = "Duplicate" }, 409);
            await Mutation("PUT", $"equipment/{E(1)}", new { equipmentCode = "EQ-001", name = " " }, 400);
            await Mutation("PUT", $"equipment/{E(1)}", new { equipmentCode = "EQ-001", name = "Camera", isAvailable = true }, 400);
            await Stop(); await Start(); Check((await Call("GET", $"equipment/{newItem}")).GetProperty("name").GetString() == "Edited Monitor", "T03 restart retains edit"); Console.WriteLine("PASS T03");

            await Reset(); Console.WriteLine("BEGIN T04 single issue and return");
            var issued = await Mutation("POST", "borrowings", Single(S(4), E(2)), 201); int loan = issued.GetProperty("id").GetInt32();
            Check(issued.GetProperty("status").GetString() == "Active" && issued.GetProperty("student").GetProperty("id").GetInt32() == S(4), "active response and related display");
            Check(!(await Call("GET", $"equipment/{E(2)}")).GetProperty("isAvailable").GetBoolean(), "checkout unavailable");
            await Call("GET", $"borrowings/{loan}");
            await Mutation("POST", "borrowings", Single(S(2), E(6)), 409);
            await Mutation("POST", "borrowings", Single(S(4), E(1)), 409);
            await Mutation("POST", "borrowings", Single(2147483647, E(6)), 404);
            await Mutation("POST", "borrowings", Single(S(4), E(6), 0), 400);
            await Mutation("POST", "borrowings", new { studentId = S(4), equipmentId = E(6), expectedReturnDate = "not-a-date" }, 400);
            await Mutation("POST", "borrowings", new { studentId = S(4), equipmentId = E(6) }, 400);
            var returned = await Mutation("POST", $"borrowings/{loan}/return", new { }, 200);
            Check(returned.GetProperty("status").GetString() == "Returned" && returned.GetProperty("returnedDate").GetString() == WorkflowService.Today.ToString("yyyy-MM-dd"), "same loan returned today");
            await Mutation("POST", $"borrowings/{loan}/return", new { }, 409);
            await Stop(); await Start(); Check((await Call("GET", $"borrowings/{loan}")).GetProperty("status").GetString() == "Returned", "T04 restart retains return");
            Check((await Call("GET", $"equipment/{E(2)}")).GetProperty("isAvailable").GetBoolean(), "returned item available"); Console.WriteLine("PASS T04");

            Console.WriteLine("BEGIN T05 atomic checkout and boundaries");
            await Reset(); Check((await Mutation("POST", "checkout-requests", Multiple(S(4), E(2), E(6)), 201)).GetProperty("borrowings").GetArrayLength() == 2, "two item checkout");
            await Reset(); await Mutation("POST", "borrowings", Single(S(3), E(2)), 201);
            Check((await Call("GET", $"students/{S(3)}/borrowings")).GetArrayLength() == 3, "T05 2a exactly three");
            await Reset(); await Mutation("POST", "checkout-requests", Multiple(S(1), E(2), E(6)), 201);
            Check((await Call("GET", $"students/{S(1)}/borrowings")).EnumerateArray().Count(x => x.GetProperty("status").GetString() == "Active") == 3, "T05 2b exactly three");
            await Reset(); await Mutation("POST", "checkout-requests", Multiple(S(3), E(2), E(6)), 409);
            await Reset();
            await Mutation("POST", "checkout-requests", Multiple(S(4), E(2), E(2)), 400);
            await Mutation("POST", "checkout-requests", Multiple(S(4), E(2)), 400);
            await Mutation("POST", "checkout-requests", Multiple(S(4), E(1), E(2)), 409);
            await Mutation("POST", "checkout-requests", Multiple(S(2), E(2), E(6)), 409);
            await Mutation("POST", "checkout-requests", Multiple(S(4), E(2), 2147483647), 404);
            await Mutation("POST", "checkout-requests", new { studentId = S(4), equipmentIds = new[] { E(2), E(6) }, expectedReturnDate = WorkflowService.Today }, 400);
            await Reset(); fault.AfterSave = true;
            await Mutation("POST", "checkout-requests", Multiple(S(4), E(2), E(6)), 500);
            Check(fault.SaveTriggered, "Fault ran after actual SaveChanges, before transaction commit");
            await Reset();
            before = await Snapshot("competing checkout before");
            async Task<int> Compete()
            {
                using var response = await http!.PostAsJsonAsync("api/v1/borrowings", Single(S(4), E(2)));
                return (int)response.StatusCode;
            }
            var races = await Task.WhenAll(Compete(), Compete());
            Check(races.Order().SequenceEqual(new[] { 201, 409 }), "competing checkout: one success, one conflict");
            await Snapshot("competing checkout after");
            Check((await Call("GET", $"equipment/{E(2)}/borrowings")).GetArrayLength() == 1, "one persisted loan after contention");
            await Mutation("POST", "checkout-requests", Multiple(S(1), E(2), E(6)), 409);
            Console.WriteLine("PASS T05");

            await Reset(); Console.WriteLine("BEGIN T06 history and query semantics"); before = await Snapshot("T06 before");
            foreach (var path in new[] { $"students/{S(1)}/borrowings", $"students/{S(4)}/borrowings", $"equipment/{E(1)}/borrowings", $"equipment/{E(3)}/borrowings" }) await Call("GET", path);
            await Call("GET", "students/2147483647/borrowings", 404); await Call("GET", "equipment/2147483647/borrowings", 404);
            foreach (var search in new[] { "  aDa  ", "st-100", "CaMeR" }) Check((await Call("GET", "borrowings?search=" + Uri.EscapeDataString(search))).GetArrayLength() > 0, "partial case-insensitive search " + search);
            foreach (var status in new[] { "All", "Active", "Returned" })
            foreach (var sort in new[] { "borrowedDate", "expectedReturnDate" })
            foreach (var direction in new[] { "asc", "desc" })
            {
                var rows = (await Call("GET", $"borrowings?status={status}&sort={sort}&direction={direction}")).EnumerateArray().ToArray();
                Check(rows.All(x => status == "All" || x.GetProperty("status").GetString() == status), "status membership");
                var ordered = direction == "asc" ? rows.OrderBy(x => x.GetProperty(sort).GetString()).ThenBy(x => x.GetProperty("id").GetInt32()) : rows.OrderByDescending(x => x.GetProperty(sort).GetString()).ThenBy(x => x.GetProperty("id").GetInt32());
                Check(rows.Select(x => x.GetProperty("id").GetInt32()).SequenceEqual(ordered.Select(x => x.GetProperty("id").GetInt32())), "date order with stable ID tie-break");
            }
            Check((await Call("GET", "borrowings?search=ada&status=Returned&sort=expectedReturnDate&direction=desc")).GetArrayLength() == 1, "combined filters");
            Check((await Call("GET", "borrowings")).GetArrayLength() == 4, "reset default all");
            Check((await Call("GET", "borrowings?search=no-matches-xyz")).GetArrayLength() == 0, "empty array");
            foreach (var query in new[] { "status=invalid", "sort=invalid", "direction=invalid" }) await Call("GET", "borrowings?" + query, 400);
            Check(before == await Snapshot("T06 after"), "T06 read-only"); Console.WriteLine("PASS T06");

            await Reset(); Console.WriteLine("BEGIN T07 compiled Avalonia XAML over HTTP");
            var uiHttp = new HttpClient(new CaptureHandler()) { BaseAddress = new Uri(http!.BaseAddress!, "api/v1/") };
            await Snapshot("T07 before");
            UiSmoke.Run(new WorkflowApiClient(uiHttp), independentChange: async () =>
                await Mutation("PUT", $"equipment/{E(7)}", new { equipmentCode = "EQ-007", name = "Independent HTTP edit", description = (string?)null }, 200));
            await Snapshot("T07 after"); Console.WriteLine("PASS T07 compiled UI command demonstration; capture path comes from EQUIPMENTBORROWING_UI_CAPTURE");

            Console.WriteLine("BEGIN T08 transport, server, recovery, unknown outcome and restart");
            var originalBase = http.BaseAddress!;
            await Stop();
            try { using var response = await http.GetAsync("api/v1/equipment"); throw new Exception("Stopped API unexpectedly responded"); }
            catch (HttpRequestException) { Console.WriteLine("PASS stopped API: transport exception, no HTTP status"); }
            var offlineClient = new WorkflowApiClient(new HttpClient { BaseAddress = new Uri(originalBase, "api/v1/"), Timeout = TimeSpan.FromSeconds(2) });
            try { await offlineClient.EquipmentAsync(); throw new Exception("Offline client unexpectedly loaded"); }
            catch (C.ApiClientException e) { Check(e.Message.Contains("no HTTP response"), "desktop connection feedback"); Console.WriteLine("UI feedback: " + e.Message); }
            UiSmoke.VerifyFeedback(offlineClient, "no HTTP response");
            before = await Snapshot("T08 failed server before");
            var badConnection = new NpgsqlConnectionStringBuilder(connectionString) { Port = 1, Timeout = 1, Pooling = false }.ConnectionString;
            await Start(badConnection); var error = await Call("GET", "equipment", 500);
            Check(error.GetProperty("code").GetString() == "server_error" && !error.ToString().Contains("Host="), "safe 500 body");
            await Mutation("POST", "students", new { studentNumber = "FAIL", fullName = "Must not persist" }, 500);
            var serverClient = new WorkflowApiClient(new HttpClient { BaseAddress = new Uri(http!.BaseAddress!, "api/v1/") });
            try { await serverClient.EquipmentAsync(); throw new Exception("Bad database unexpectedly loaded"); }
            catch (C.ApiClientException e) { Check(e.Message.Contains("HTTP 500") && !e.Message.Contains("no HTTP response"), "desktop server feedback distinct"); Console.WriteLine("UI feedback: " + e.Message); }
            UiSmoke.VerifyFeedback(serverClient, "HTTP 500");
            await Stop(); await Start(); await Call("GET", "equipment");
            Check(before == await Snapshot("T08 after recovery"), "committed UI data survives restart and failed server");
            fault.AfterCommit = true;
            var unknown = await Call("POST", "students", 500, new { studentNumber = "UNKNOWN-001", fullName = "Acknowledgement lost" });
            Check(unknown.GetProperty("code").GetString() == "outcome_unknown" && fault.CommitTriggered, "lost commit acknowledgement classified unknown");
            await Snapshot("T08 unknown commit fresh verification before retry");
            Check((await Call("GET", "students?search=UNKNOWN-001")).GetArrayLength() == 1, "fresh read resolves committed outcome; no resubmission");
            Console.WriteLine("PASS T08 host restart; run separate-process --readback as documented");
            Console.WriteLine("PASS T01–T08 execution complete. Database retained: " + database);
            return 0;
        }
        catch (Exception e)
        {
            // Assertions are intentionally safe; database exceptions may contain private connection details.
            Console.Error.WriteLine("FAIL execution: " + (e is NpgsqlException ? "PostgreSQL connection/setup failed; check local configuration." : e.Message)); return 1;
        }
        finally { await Stop(); http?.Dispose(); }
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message); Console.WriteLine("CHECK " + message);
    }
    private sealed class CaptureHandler : DelegatingHandler
    {
        public CaptureHandler() : base(new HttpClientHandler()) { }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Console.WriteLine($"HTTP {DateTimeOffset.UtcNow:O} {request.Method} {request.RequestUri} { (request.Content is null ? "" : await request.Content.ReadAsStringAsync(token)) }");
            var response = await base.SendAsync(request, token);
            Console.WriteLine($"RESPONSE {(int)response.StatusCode} Location={response.Headers.Location} {await response.Content.ReadAsStringAsync(token)}"); return response;
        }
    }
    private sealed class Factory(string connection, Faults faults) : IDbContextFactory<EquipmentBorrowingDbContext>
    {
        public EquipmentBorrowingDbContext CreateDbContext() => new(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection).AddInterceptors(faults.Save, faults.Commit).Options);
    }
    private sealed class Faults
    {
        public bool AfterSave, AfterCommit, SaveTriggered, CommitTriggered;
        public SaveFault Save => new(this);
        public CommitFault Commit => new(this);
    }
    private sealed class SaveFault(Faults faults) : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken cancellationToken = default)
        {
            if (faults.AfterSave) { faults.AfterSave = false; faults.SaveTriggered = true; throw new NpgsqlException("Controlled failure after writes, before commit."); }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class CommitFault(Faults faults) : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (faults.AfterCommit) { faults.AfterCommit = false; faults.CommitTriggered = true; throw new NpgsqlException("Controlled lost commit acknowledgement."); }
            return Task.CompletedTask;
        }
    }
}
