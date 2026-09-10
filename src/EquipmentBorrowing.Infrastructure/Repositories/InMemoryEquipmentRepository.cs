using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment = new();

    public InMemoryEquipmentRepository()
    {
        _equipment.Add(new Equipment(1, "Projector", true));
        _equipment.Add(new Equipment(2, "Laptop - Dell XPS", true));
        _equipment.Add(new Equipment(3, "Microphone Set", true));
        _equipment.Add(new Equipment(4, "Digital Camera", true));
    }

    public void Add(Equipment equipment) => _equipment.Add(equipment);

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.FirstOrDefault(e => e.Id == id));

    public Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.ToList());

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}