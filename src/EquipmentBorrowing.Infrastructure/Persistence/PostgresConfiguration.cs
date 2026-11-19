
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class PostgresConfiguration
{
    private const string EnvironmentVariableName =
        "EQUIPMENT_BORROWING_DB";

    public static string ConnectionString
    {
        get
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
                    EnvironmentVariableName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string is missing. " +
                    "Set the EQUIPMENT_BORROWING_DB " +
                    "environment variable before running " +
                    "the application or EF Core migrations.");
            }

            return connectionString;
        }
    }
}

public sealed class EquipmentBorrowingDbContextFactory
    : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(
        string[] args)
    {
        var options =
            new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
                .UseNpgsql(PostgresConfiguration.ConnectionString)
                .Options;

        return new EquipmentBorrowingDbContext(options);
    }
}
