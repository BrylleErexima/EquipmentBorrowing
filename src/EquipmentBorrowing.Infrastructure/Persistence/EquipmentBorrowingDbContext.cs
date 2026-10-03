using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class EquipmentBorrowingDbContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();

    public EquipmentBorrowingDbContext(DbContextOptions<EquipmentBorrowingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EquipmentBorrowingDbContext).Assembly);

        // Seed Initial Data matching updated Domain entities
        modelBuilder.Entity<Student>().HasData(
            new Student(1, "Ana Reyes", canBorrow: true, maxActiveBorrowings: 2),
            new Student(2, "Juan Dela Cruz", canBorrow: true, maxActiveBorrowings: 2)
        );

        modelBuilder.Entity<Equipment>().HasData(
            new Equipment(1, "Dell XPS 15 Laptop", isAvailable: true),
            new Equipment(2, "Canon EOS 80D Camera", isAvailable: true),
            new Equipment(3, "Epson HD Projector", isAvailable: true)
        );
    }
}