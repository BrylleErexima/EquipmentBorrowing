using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.StudentName).IsRequired().HasMaxLength(100);
        builder.Property(b => b.EquipmentName).IsRequired().HasMaxLength(100);
        builder.Property(b => b.DateBorrowed).IsRequired();
        builder.Property(b => b.ExpectedReturnDate).IsRequired();
        builder.Property(b => b.Status).IsRequired();
    }
}