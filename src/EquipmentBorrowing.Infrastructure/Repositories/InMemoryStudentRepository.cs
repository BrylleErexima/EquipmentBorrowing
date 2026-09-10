using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = new()
    {
        new Student(1, "Alice Smith", true, 3),
        new Student(2, "Bob Johnson", true, 3),
        new Student(3, "Charlie Brown", true, 3)
    };

    public void Add(Student student)
    {
        _students.Add(student);
    }

    public Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_students);
    public InMemoryStudentRepository()
    {
        _students.Add(new Student(1, "Juan Dela Cruz"));
        _students.Add(new Student(2, "Maria Santos"));
        _students.Add(new Student(3, "Pedro Reyes"));
    }

    public void Add(Student student) => _students.Add(student);

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_students.FirstOrDefault(s => s.Id == id));
}