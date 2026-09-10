using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment = new()
    {
        new Equipment(1, "Laptop - Dell XPS", true),
        new Equipment(2, "Projector - Epson", true),
        new Equipment(3, "DSLR Camera - Canon", true)
    };

    public void Add(Equipment equipment)
    {
        _equipment.Add(equipment);
    }

    public Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment);

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.FirstOrDefault(e => e.Id == id));

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}