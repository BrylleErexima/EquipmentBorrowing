using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment = new();

    public void Add(Equipment equipment) => _equipment.Add(equipment);

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.FirstOrDefault(e => e.Id == id));

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
