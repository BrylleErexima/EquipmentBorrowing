using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

var students = new InMemoryStudentRepository();
var equipment = new InMemoryEquipmentRepository();
var borrowings = new InMemoryBorrowingRepository(
    equipment,
    students);

students.Add(new Student(1, "Juan Dela Cruz"));
students.Add(new Student(2, "Maria Santos", canBorrow: false));
equipment.Add(new Equipment(1, "Laptop"));
equipment.Add(new Equipment(2, "Projector", isAvailable: false));

var service = new BorrowEquipmentService(students, equipment, borrowings);

Console.WriteLine("Campus Equipment Borrowing");
Console.WriteLine("--------------------------");

var success = await service.BorrowAsync(1, 1, DateTime.Today.AddDays(7));
Console.WriteLine($"Success case: {success.Message}");

var failure = await service.BorrowAsync(1, 2, DateTime.Today.AddDays(7));
Console.WriteLine($"Failure case: {failure.Message}");

if (!success.Success || failure.Success)
{
    Console.WriteLine("Demo checks failed.");
    return 1;
}

Console.WriteLine("All checks passed.");
return 0;
