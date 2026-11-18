using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace EquipmentBorrowing.Infrastructure.Persistence;
public static class PostgresConfiguration
{
    public static string ConnectionString => Environment.GetEnvironmentVariable("EQUIPMENTBORROWING_CONNECTION")
        ?? "Host=localhost;Database=equipment_borrowing;Username=postgres";
}
public sealed class EquipmentBorrowingDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(PostgresConfiguration.ConnectionString).Options);
}
