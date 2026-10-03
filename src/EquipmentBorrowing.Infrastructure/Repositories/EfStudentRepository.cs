using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfStudentRepository : IStudentRepository
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfStudentRepository(EquipmentBorrowingDbContext context) => _context = context;

    public async Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Students.AsNoTracking().ToListAsync(cancellationToken);
    }
}