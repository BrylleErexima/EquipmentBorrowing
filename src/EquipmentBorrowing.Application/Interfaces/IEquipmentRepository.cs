using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
}