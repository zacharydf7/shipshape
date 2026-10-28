using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Spike.Tests.Toys.Checklist;

[DbContext(typeof(ChecklistContext))]
[Migration("20260202090000_CreateItems")]
public class CreateItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Items",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Done = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            },
            constraints: table => table.PrimaryKey("PK_Items", x => x.Id));
    }
}

[DbContext(typeof(ChecklistContext))]
[Migration("20260202090100_AddItemPriority")]
public class AddItemPriority : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Priority", table: "Items", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.SqlExec("UPDATE [Items] SET [Priority] = 2, [Text] = [Text] + N' (it''s open)' WHERE [Done] = 0;");
    }
}

[DbContext(typeof(ChecklistContext))]
[Migration("20260202090200_CreateOpenItemsView")]
public class CreateOpenItemsView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.SqlExec("""
            CREATE VIEW [OpenItems] AS
            SELECT [Id], [Text], [Priority] FROM [Items] WHERE [Done] = 0;
            """);
    }
}
