using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Tests.Toys.Drift;

[DbContext(typeof(DriftContext))]
[Migration("20260209090000_CreateNotes")]
public class CreateNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Notes", x => x.Id));
    }
}

// The model as of CreateNotes. Note.Pinned was added to the model afterward without a migration.
[DbContext(typeof(DriftContext))]
public class DriftContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14").HasAnnotation("Relational:MaxIdentifierLength", 128);
        modelBuilder.UseIdentityColumns();
        modelBuilder.Entity("Shipshape.Tests.Toys.Note", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<string>("Text").IsRequired().HasColumnType("nvarchar(max)");
            b.HasKey("Id");
            b.ToTable("Notes");
        });
    }
}
