using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

var adminString = Environment.GetEnvironmentVariable("EQUIPMENTBORROWING_TEST_CONNECTION");
if (args.Length != 0 && (args.Length != 2 || args[0] != "--readback")) { Console.Error.WriteLine("Usage: no arguments, or --readback equipment_lab4_test_<32 hex digits>"); return 2; }
if (string.IsNullOrWhiteSpace(adminString)) { Console.Error.WriteLine("Set EQUIPMENTBORROWING_TEST_CONNECTION to a disposable PostgreSQL admin connection with CREATEDB permission. See README."); return 2; }
var builder = new NpgsqlConnectionStringBuilder(adminString);
var database = args.Length == 2 && args[0] == "--readback" ? args[1] : "equipment_lab4_test_" + Guid.NewGuid().ToString("N");
if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^equipment_lab4_test_[a-f0-9]{32}$")) throw new Exception("Unsafe database name.");
if (args.Length == 0) {
    await using var admin = new NpgsqlConnection(adminString); await admin.OpenAsync();
    await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin); await cmd.ExecuteNonQueryAsync();
}
builder.Database = database;
var factory = new TestFactory(builder.ConnectionString);
var service = new WorkflowService(new PostgresWorkflowRepository(factory));
int passed = 0;
void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); Console.WriteLine("PASS: " + label); passed++; }
async Task Expect(bool expected, Task<OperationResult> task, string label) { var r = await task; Check(r.Success == expected && !r.OutcomeUnknown, label + " [" + r.Message + "]"); }
async Task<string> Snapshot() {
    await using var db = factory.CreateDbContext();
    var students = await db.Students.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.StudentNumber, x.Name, x.Contact, x.CanBorrow, x.Version }).ToListAsync();
    var equipment = await db.Equipments.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Identifier, x.Name, x.Description, x.IsAvailable, x.Version }).ToListAsync();
    var loans = await db.Borrowings.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.StudentId, x.EquipmentId, x.DateBorrowed, x.ExpectedReturnDate, x.ActualReturnDate }).ToListAsync();
    return System.Text.Json.JsonSerializer.Serialize(new { students, equipment, loans });
}
async Task Reject(Func<Task<OperationResult>> task, string label) { var before = await Snapshot(); await Expect(false, task(), label); Check(await Snapshot() == before, label + " / fresh read unchanged"); }
async Task Constraint(string sql, string state, string label) {
    await using var db = factory.CreateDbContext();
    try { await db.Database.ExecuteSqlRawAsync(sql); throw new Exception("Invalid write accepted: " + label); }
    catch (PostgresException e) { Check(e.SqlState == state, label); }
}
Console.WriteLine("Database: " + database);
if (args.Length > 0) {
    var rows = await service.BorrowingsAsync(new());
    Check(rows.Any(x => x.ActualReturnDate is not null) && rows.Any(x => x.ActualReturnDate is null), "Restart readback: active and returned history");
    Check((await service.StudentsAsync()).Any(x => x.Name == "Edited Student"), "Restart readback: edited student");
    Check((await service.EquipmentAsync()).Any(x => x.Name == "Edited Laptop"), "Restart readback: edited equipment");
    Console.WriteLine($"{passed} restart checks passed."); return 0;
}
await using (var db = factory.CreateDbContext()) { await db.Database.MigrateAsync(); Check((await db.Database.GetAppliedMigrationsAsync()).Count() == 1, "A17 clean PostgreSQL migration"); }
Check((await service.StudentsAsync()).Count == 3 && (await service.EquipmentAsync()).Count == 5, "A01/A02 seed catalog");
await using (var db = factory.CreateDbContext()) { await db.Database.MigrateAsync(); }
Check((await service.EquipmentAsync()).Count == 5, "Reapplying migration does not repeat seed");
await Expect(true, service.SaveStudentAsync(null, 0, " LAB-100 ", "New Student", "contact"), "A12 register");
await Expect(true, service.SaveStudentAsync(null, 0, "LAB-200", "Other Student", null), "A12 second student");
await Expect(true, service.SaveStudentAsync(null, 0, "LAB-300", "Empty History", null), "A07 empty subject");
var student = (await service.StudentsAsync()).Single(x => x.StudentNumber == "LAB-100");
var other = (await service.StudentsAsync()).Single(x => x.StudentNumber == "LAB-200");
Check(student.CanBorrow && student.Contact == "contact", "A12 trimming and eligibility default");
await Reject(() => service.SaveStudentAsync(null, 0, " lab-100 ", "Dup", null), "A12 normalized duplicate");
await Reject(() => service.SaveStudentAsync(null, 0, "", "Name", null), "A12 blank number");
await Reject(() => service.SaveStudentAsync(null, 0, "X", " ", null), "A12 blank name");
await Reject(() => service.SaveStudentAsync(null, 0, "CONTACT", "Name", "   "), "A12 supplied whitespace-only contact");
await Expect(true, service.SaveEquipmentAsync(null, 0, " LAB-EQ ", "New Item", " "), "A15 register");
var newItem = (await service.EquipmentAsync()).Single(x => x.Identifier == "LAB-EQ");
Check(newItem.IsAvailable && newItem.Description is null, "A15 defaults and optional description");
await Reject(() => service.SaveEquipmentAsync(null, 0, "lab-eq", "Dup", null), "A15 normalized duplicate");
await Reject(() => service.SaveEquipmentAsync(null, 0, "", "Name", null), "A15 blank identifier");
await Reject(() => service.SaveEquipmentAsync(null, 0, "X", " ", null), "A15 blank name");
var items = (await service.EquipmentAsync()).Where(x => x.Identifier.StartsWith("EQ-")).OrderBy(x => x.Id).ToArray();
var due = WorkflowService.Today.AddDays(7);
await Expect(true, service.CheckoutAsync(student.Id, [items[0].Id], due, false), "A03 single checkout");
Check(!(await service.EquipmentAsync()).Single(x => x.Id == items[0].Id).IsAvailable, "A03 unavailable readback");
var loan = (await service.BorrowingsAsync(new(StudentId: student.Id))).Single();
Check(loan.Student.Id == student.Id && loan.Equipment.Id == items[0].Id, "A04 related identities");
await Reject(() => service.CheckoutAsync(student.Id, [items[0].Id], due, false), "A03 unavailable");
await Reject(() => service.CheckoutAsync(999999, [items[1].Id], due, false), "A03 missing student");
await Reject(() => service.CheckoutAsync(student.Id, [999999], due, false), "A03 missing item");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id], WorkflowService.Today, false), "A03 invalid date");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id], null, false), "A03 absent date");
await Expect(true, service.SaveStudentAsync(student.Id, student.Version, "LAB-101", "Edited Student", " "), "A13 edit");
var stale = student; student = (await service.StudentsAsync()).Single(x => x.Id == student.Id);
Check(student.Contact is null && (await service.BorrowingsAsync(new(StudentId: student.Id))).Single().Id == loan.Id, "A13 clear contact and preserve history");
await Reject(() => service.SaveStudentAsync(student.Id, stale.Version, "OLD", "Stale", null), "A13 stale edit");
await Reject(() => service.SaveStudentAsync(student.Id, student.Version, other.StudentNumber, "Name", null), "A13 duplicate edit");
await Reject(() => service.SaveStudentAsync(student.Id, student.Version, "", "Name", null), "A13 invalid edit");
var item = (await service.EquipmentAsync()).Single(x => x.Id == items[0].Id);
await Expect(true, service.SaveEquipmentAsync(item.Id, item.Version, "EQ-EDITED", "Edited Laptop", ""), "A16 edit while on loan");
var edited = (await service.EquipmentAsync()).Single(x => x.Id == item.Id);
Check(!edited.IsAvailable && edited.Description is null && (await service.BorrowingsAsync(new(EquipmentId: item.Id))).Single().Id == loan.Id, "A16 protected availability and history");
await Reject(() => service.SaveEquipmentAsync(edited.Id, edited.Version, items[1].Identifier, "Name", null), "A16 duplicate edit");
await Reject(() => service.SaveEquipmentAsync(edited.Id, edited.Version, "", "Name", null), "A16 invalid edit");
await Reject(() => service.SaveEquipmentAsync(edited.Id, item.Version, "NEW", "Stale", null), "A16 stale edit");
await Expect(true, service.SetEligibilityAsync(student.Id, student.Version, false), "A14 restrict active borrower");
student = (await service.StudentsAsync()).Single(x => x.Id == student.Id);
await Expect(true, service.SetEligibilityAsync(student.Id, student.Version, false), "A14 repeated state");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id], due, false), "A03/A14 restricted");
Check((await service.BorrowingsAsync(new(StudentId: student.Id, Status: StatusFilter.Active))).Count == 1, "A14 active loan retained");
await Expect(true, service.ReturnAsync(loan.Id), "A05 restricted student early return");
Check((await service.BorrowingsAsync(new(StudentId: student.Id))).Single().ActualReturnDate == WorkflowService.Today && (await service.EquipmentAsync()).Single(x => x.Id == item.Id).IsAvailable, "A05 return date and availability readback");
await Reject(() => service.ReturnAsync(loan.Id), "A05 repeated return");
await Reject(() => service.ReturnAsync(999999), "A05 missing loan");
await Expect(true, service.SetEligibilityAsync(student.Id, student.Version, true), "A14 permit");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id], due, true), "A06 fewer than two");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id, items[1].Id], due, true), "A06 duplicate items");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id, 999999], due, true), "A06 missing item");
await Reject(() => service.CheckoutAsync(student.Id, [items[1].Id, items[2].Id], null, true), "A06 invalid date");
await Expect(true, service.CheckoutAsync(student.Id, [items[1].Id, items[2].Id], due.AddDays(1), true), "A06 two-item checkout");
Check((await service.BorrowingsAsync(new(StudentId: student.Id, Status: StatusFilter.Active))).Count == 2, "A06 both rows committed");
await Reject(() => service.CheckoutAsync(other.Id, [items[1].Id, items[3].Id], due, true), "A06 unavailable plus available");
await Reject(() => service.CheckoutAsync(student.Id, [items[3].Id, items[4].Id], due, true), "A06 over limit");
await Expect(true, service.CheckoutAsync(student.Id, [items[3].Id], due, false), "A03 exactly three active items");
await Reject(() => service.CheckoutAsync(student.Id, [items[4].Id], due, false), "A03 over limit");
var failure = new FailAfterWrite();
var failingService = new WorkflowService(new PostgresWorkflowRepository(new TestFactory(builder.ConnectionString, failure)));
await Reject(() => failingService.CheckoutAsync(other.Id, [items[4].Id, newItem.Id], due, true), "A06/A18 rollback after SQL write");
Check(failure.WritesObserved, "A06 failure seam observed SQL writes before pre-commit exception");
var rivals = await Task.WhenAll(service.CheckoutAsync(other.Id, [items[4].Id], due, false), service.CheckoutAsync(other.Id, [items[4].Id], due, false));
Check(rivals.Count(x => x.Success) == 1, "A06 concurrent item checkout: exactly one winner");
Check((await service.BorrowingsAsync(new(EquipmentId: items[4].Id, Status: StatusFilter.Active))).Count == 1, "A06 no duplicate active item");
var all = await service.BorrowingsAsync(new());
foreach (var text in new[] { " edited student ", " lab-101 ", " edited laptop " }) Check((await service.BorrowingsAsync(new(Search: text))).Count > 0, "A09 partial case-insensitive search " + text.Trim());
foreach (var filter in Enum.GetValues<StatusFilter>()) {
    var rows = await service.BorrowingsAsync(new(Status: filter));
    Check(rows.All(x => filter == StatusFilter.All || (x.ActualReturnDate is null) == (filter == StatusFilter.Active)), "A09 status " + filter);
}
foreach (var sort in Enum.GetValues<BorrowingSort>()) foreach (var desc in new[] { true, false }) {
    var rows = await service.BorrowingsAsync(new(Sort: sort, Descending: desc));
    Func<Borrowing, DateOnly> key = sort == BorrowingSort.CheckoutDate ? x => x.DateBorrowed : x => x.ExpectedReturnDate;
    var expected = desc ? all.OrderByDescending(key).ThenBy(x => x.Id) : all.OrderBy(key).ThenBy(x => x.Id);
    Check(rows.Select(x => x.Id).SequenceEqual(expected.Select(x => x.Id)), $"A09 {sort} descending={desc} / stable ID tie-breaker");
}
Check((await service.BorrowingsAsync(new(Search: "lab-101", Status: StatusFilter.Returned))).Count == 1, "A09 combined criteria");
Check((await service.BorrowingsAsync(new(Search: "no-matches-xyz"))).Count == 0, "A09 no matches");
Check((await service.BorrowingsAsync(new(Search: "%_"))).Count == 0, "A09 wildcard characters are literal search text");
Check((await service.BorrowingsAsync(new(StudentId: student.Id))).All(x => x.StudentId == student.Id), "A07 isolated student history");
Check((await service.BorrowingsAsync(new(EquipmentId: items[4].Id))).All(x => x.EquipmentId == items[4].Id), "A08 isolated equipment history");
var empty = (await service.StudentsAsync()).Single(x => x.StudentNumber == "LAB-300");
Check((await service.BorrowingsAsync(new(StudentId: empty.Id))).Count == 0 && (await service.BorrowingsAsync(new(EquipmentId: newItem.Id))).Count == 0, "A07/A08 empty histories");
foreach (var q in new[] { new BorrowingQuery(StudentId: 999999), new BorrowingQuery(EquipmentId: 999999) }) {
    try { await service.BorrowingsAsync(q); throw new Exception("Missing history subject accepted"); } catch (WorkflowException) { Check(true, "A07/A08 missing differs from empty"); }
}
await Constraint("INSERT INTO \"Students\" (\"Name\",\"StudentNumber\",\"CanBorrow\",\"Version\") VALUES ('Dup',' lab-101 ',true,1)", "23505", "A17 normalized student unique constraint");
await Constraint("INSERT INTO \"Equipments\" (\"Name\",\"Identifier\",\"IsAvailable\",\"Version\") VALUES ('Dup',' lab-eq ',true,1)", "23505", "A17 normalized equipment unique constraint");
await Constraint("INSERT INTO \"Students\" (\"Name\",\"StudentNumber\",\"CanBorrow\",\"Version\") VALUES (' ','BLANK',true,1)", "23514", "A17 required nonblank check");
await Constraint($"INSERT INTO \"Borrowings\" (\"StudentId\",\"EquipmentId\",\"DateBorrowed\",\"ExpectedReturnDate\") VALUES (999999,{newItem.Id},CURRENT_DATE,CURRENT_DATE+1)", "23503", "A17 enforced FK");
await Constraint($"INSERT INTO \"Borrowings\" (\"StudentId\",\"EquipmentId\",\"DateBorrowed\",\"ExpectedReturnDate\") VALUES ({other.Id},{items[4].Id},CURRENT_DATE,CURRENT_DATE+1)", "23505", "A17 partial unique active-item constraint");
var offlineBuilder = new NpgsqlConnectionStringBuilder(builder.ConnectionString) { Port = 1, Timeout = 1 };
var offline = new WorkflowService(new PostgresWorkflowRepository(new TestFactory(offlineBuilder.ConnectionString)));
try { await offline.EquipmentAsync(); throw new Exception("Offline read succeeded"); } catch (PersistenceException e) { Check(!e.Message.Contains("Host="), "A01/A02/A18 secret-free load failure"); }
await Expect(false, offline.CheckoutAsync(other.Id, [newItem.Id], due, false), "A18 offline write no false success");
Check((await service.EquipmentAsync()).Count > 0, "A18 recovered connection works");
await Constraint("INSERT INTO \"Students\" (\"Name\",\"StudentNumber\",\"CanBorrow\",\"Version\") VALUES ('Dup',E'\\tLAB-101\\n',true,1)", "23505", "A17 tab/newline identifier normalization matches Trim");
await Constraint("INSERT INTO \"Students\" (\"Name\",\"StudentNumber\",\"CanBorrow\",\"Version\") VALUES (E'\\t','WHITESPACE',true,1)", "23514", "A17 whitespace-only name rejected");
var unknownService = new WorkflowService(new PostgresWorkflowRepository(new TestFactory(builder.ConnectionString, new LoseCommitAcknowledgement())));
var uncertain = await unknownService.CheckoutAsync(other.Id, [newItem.Id], due, false);
Check(!uncertain.Success && uncertain.OutcomeUnknown && uncertain.Message.Contains("Do not resubmit"), "A18 lost commit acknowledgement reports unknown, never false rollback");
Check((await service.BorrowingsAsync(new(EquipmentId: newItem.Id, Status: StatusFilter.Active))).Count == 1, "A18 fresh read resolves committed outcome without retry");
var restricted = (await service.StudentsAsync()).Single(x => x.StudentNumber == "DEMO-003");
await Reject(() => service.CheckoutAsync(restricted.Id, [items[0].Id, newItem.Id], due, true), "A06 ineligible student");
await Reject(() => service.CheckoutAsync(999999, [items[0].Id, newItem.Id], due, true), "A06 missing student");
await Reject(() => service.CheckoutAsync(other.Id, [], due, true), "A06 empty selection");
for (var i = 1; i <= 4; i++) await Expect(true, service.SaveEquipmentAsync(null, 0, $"LIMIT-{i}", $"Limit Item {i}", null), "A06 limit fixture registration");
var limitItems = (await service.EquipmentAsync()).Where(x => x.Identifier.StartsWith("LIMIT-")).ToArray();
await Expect(true, service.CheckoutAsync(empty.Id, [limitItems[0].Id], due, false), "A06 one existing active item");
await Expect(true, service.CheckoutAsync(empty.Id, [limitItems[1].Id, limitItems[2].Id], due, true), "A06 coordinated checkout exactly at three-item limit");
Check((await service.BorrowingsAsync(new(StudentId: empty.Id, Status: StatusFilter.Active))).Count == 3, "A06 limit readback counts all requested items");
var returnForRace = (await service.BorrowingsAsync(new(EquipmentId: limitItems[2].Id, Status: StatusFilter.Active))).Single();
await Expect(true, service.ReturnAsync(returnForRace.Id), "A06 prepare two active items for limit race");
var limitRace = await Task.WhenAll(service.CheckoutAsync(empty.Id, [limitItems[2].Id], due, false), service.CheckoutAsync(empty.Id, [limitItems[3].Id], due, false));
Check(limitRace.Count(x => x.Success) == 1 && (await service.BorrowingsAsync(new(StudentId: empty.Id, Status: StatusFilter.Active))).Count == 3,
    "A06 concurrent different-item requests cannot exceed student limit");
UiSmoke.Run(service);
Console.WriteLine($"{passed} checks passed. NEW PROCESS: --readback {database}");
return 0;

sealed class TestFactory(string connection, IInterceptor? interceptor = null) : IDbContextFactory<EquipmentBorrowingDbContext> {
    public EquipmentBorrowingDbContext CreateDbContext() {
        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection);
        if (interceptor is not null) options.AddInterceptors(interceptor); return new(options.Options);
    }
}
sealed class LoseCommitAcknowledgement : DbTransactionInterceptor {
    public override Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        => throw new System.IO.IOException("Controlled loss of acknowledgement after server commit");
}
sealed class FailAfterWrite : SaveChangesInterceptor {
    public bool WritesObserved { get; private set; }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken cancellationToken = default) {
        WritesObserved = result > 0; throw new System.IO.IOException("Controlled post-write pre-commit test failure");
    }
}
