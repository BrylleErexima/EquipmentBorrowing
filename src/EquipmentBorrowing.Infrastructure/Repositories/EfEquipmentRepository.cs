using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfEquipmentRepository : IEquipmentRepository
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfEquipmentRepository(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // AsNoTracking used for read-only view performance
        return await _context.Equipments.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        _context.Equipments.Update(equipment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}