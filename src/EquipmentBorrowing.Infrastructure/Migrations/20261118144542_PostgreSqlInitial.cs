using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquipmentBorrowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PostgreSqlInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    NormalizedIdentifier = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(btrim(\"Identifier\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288)))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.CheckConstraint("CK_Equipment_Description", "\"Description\" IS NULL OR length(btrim(\"Description\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
                    table.CheckConstraint("CK_Equipment_Required", "length(btrim(\"Name\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0 AND length(btrim(\"Identifier\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StudentNumber = table.Column<string>(type: "text", nullable: false),
                    Contact = table.Column<string>(type: "text", nullable: true),
                    CanBorrow = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    NormalizedNumber = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(btrim(\"StudentNumber\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288)))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.CheckConstraint("CK_Student_Contact", "\"Contact\" IS NULL OR length(btrim(\"Contact\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
                    table.CheckConstraint("CK_Student_Required", "length(btrim(\"Name\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0 AND length(btrim(\"StudentNumber\", chr(9) || chr(10) || chr(11) || chr(12) || chr(13) || chr(32) || chr(133) || chr(160) || chr(5760) || chr(8192) || chr(8193) || chr(8194) || chr(8195) || chr(8196) || chr(8197) || chr(8198) || chr(8199) || chr(8200) || chr(8201) || chr(8202) || chr(8232) || chr(8233) || chr(8239) || chr(8287) || chr(12288))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Borrowings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    DateBorrowed = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualReturnDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Borrowings", x => x.Id);
                    table.CheckConstraint("CK_Borrowing_Expected", "\"ExpectedReturnDate\" > \"DateBorrowed\"");
                    table.CheckConstraint("CK_Borrowing_Return", "\"ActualReturnDate\" IS NULL OR \"ActualReturnDate\" >= \"DateBorrowed\"");
                    table.ForeignKey(
                        name: "FK_Borrowings_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Borrowings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Initial examples are inserted only when this migration is applied; identity sequences remain valid.
            migrationBuilder.Sql("""
                INSERT INTO "Students" ("Name", "StudentNumber", "Contact", "CanBorrow", "Version") VALUES
                ('Ana Reyes', 'DEMO-001', NULL, TRUE, 1),
                ('Juan Dela Cruz', 'DEMO-002', NULL, TRUE, 1),
                ('Restricted Demo Student', 'DEMO-003', NULL, FALSE, 1);
                INSERT INTO "Equipments" ("Identifier", "Name", "Description", "IsAvailable", "Version") VALUES
                ('EQ-001', 'Dell Laptop', 'Laptop computer', TRUE, 1),
                ('EQ-002', 'Canon Camera', 'Digital camera', TRUE, 1),
                ('EQ-003', 'Epson Projector', 'HD projector', TRUE, 1),
                ('EQ-004', 'Tripod', NULL, TRUE, 1),
                ('EQ-005', 'Microphone', NULL, TRUE, 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_EquipmentId",
                table: "Borrowings",
                column: "EquipmentId",
                unique: true,
                filter: "\"ActualReturnDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_StudentId",
                table: "Borrowings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_NormalizedIdentifier",
                table: "Equipments",
                column: "NormalizedIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_NormalizedNumber",
                table: "Students",
                column: "NormalizedNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Borrowings");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
