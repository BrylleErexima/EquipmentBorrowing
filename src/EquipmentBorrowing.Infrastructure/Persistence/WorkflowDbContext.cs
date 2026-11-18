using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
namespace EquipmentBorrowing.Infrastructure.Persistence;
public sealed class EquipmentBorrowingDbContext(DbContextOptions<EquipmentBorrowingDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();
    // Match .NET String.Trim's Unicode whitespace set in PostgreSQL constraints/normalization.
    public static string TrimSql(string column) => "btrim(\"" + column + "\", " +
        string.Join(" || ", new[] { 9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288 }.Select(x => $"chr({x})")) + ")";
    protected override void OnModelCreating(ModelBuilder m)
    {
        var s = m.Entity<Student>();
        s.HasKey(x => x.Id); s.Property(x => x.Name).IsRequired(); s.Property(x => x.StudentNumber).IsRequired();
        s.Property(x => x.Version).IsConcurrencyToken();
        s.Property<string>("NormalizedNumber").HasComputedColumnSql("lower(" + TrimSql("StudentNumber") + ")", stored: true);
        s.HasIndex("NormalizedNumber").IsUnique();
        s.ToTable("Students", t =>
        {
            t.HasCheckConstraint("CK_Student_Required", $"length({TrimSql("Name")}) > 0 AND length({TrimSql("StudentNumber")}) > 0");
            t.HasCheckConstraint("CK_Student_Contact", $"\"Contact\" IS NULL OR length({TrimSql("Contact")}) > 0");
        });
        var e = m.Entity<Equipment>();
        e.HasKey(x => x.Id); e.Property(x => x.Name).IsRequired(); e.Property(x => x.Identifier).IsRequired();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.Property<string>("NormalizedIdentifier").HasComputedColumnSql("lower(" + TrimSql("Identifier") + ")", stored: true);
        e.HasIndex("NormalizedIdentifier").IsUnique();
        e.ToTable("Equipments", t =>
        {
            t.HasCheckConstraint("CK_Equipment_Required", $"length({TrimSql("Name")}) > 0 AND length({TrimSql("Identifier")}) > 0");
            t.HasCheckConstraint("CK_Equipment_Description", $"\"Description\" IS NULL OR length({TrimSql("Description")}) > 0");
        });
        var b = m.Entity<Borrowing>();
        b.HasKey(x => x.Id); b.Ignore(x => x.Status);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.EquipmentId).IsUnique().HasFilter("\"ActualReturnDate\" IS NULL");
        b.ToTable("Borrowings", t =>
        {
            t.HasCheckConstraint("CK_Borrowing_Expected", "\"ExpectedReturnDate\" > \"DateBorrowed\"");
            t.HasCheckConstraint("CK_Borrowing_Return", "\"ActualReturnDate\" IS NULL OR \"ActualReturnDate\" >= \"DateBorrowed\"");
        });
    }
}
