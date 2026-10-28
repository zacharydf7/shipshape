using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Spike.Tests.Toys.Library;

[DbContext(typeof(LibraryContext))]
[Migration("20260105090000_CreateAuthors")]
public class CreateAuthors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Authors",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Born = table.Column<DateOnly>(type: "date", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_Authors", x => x.Id));
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090100_CreateBooks")]
public class CreateBooks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Books",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                AuthorId = table.Column<int>(type: "int", nullable: false),
                Isbn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Books", x => x.Id);
                table.ForeignKey("FK_Books_Authors_AuthorId", x => x.AuthorId, "Authors", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Books_AuthorId", table: "Books", column: "AuthorId");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090200_AddBookIsbnIndex")]
public class AddBookIsbnIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_Books_Isbn", table: "Books", column: "Isbn", unique: true, filter: "[Isbn] IS NOT NULL");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090300_AddBookPublishedYear")]
public class AddBookPublishedYear : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "PublishedYear", table: "Books", type: "int", nullable: true);
        migrationBuilder.AddCheckConstraint(name: "CK_Books_PublishedYear", table: "Books", sql: "[PublishedYear] >= 1450");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090400_CreateMembers")]
public class CreateMembers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Members",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                JoinedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
            },
            constraints: table => table.PrimaryKey("PK_Members", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_Members_Email", table: "Members", column: "Email", unique: true);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090500_CreateLoans")]
public class CreateLoans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Loans",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                BookId = table.Column<int>(type: "int", nullable: false),
                MemberId = table.Column<int>(type: "int", nullable: false),
                LoanedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                DueOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReturnedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Loans", x => x.Id);
                table.ForeignKey("FK_Loans_Books_BookId", x => x.BookId, "Books", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Loans_Members_MemberId", x => x.MemberId, "Members", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Loans_BookId", table: "Loans", column: "BookId");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090600_AddLoanIsOpen")]
public class AddLoanIsOpen : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsOpen", table: "Loans", type: "bit", nullable: true,
            computedColumnSql: "CAST(CASE WHEN [ReturnedOn] IS NULL THEN 1 ELSE 0 END AS bit)", stored: false);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090700_AddMemberDisplayName")]
public class AddMemberDisplayName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DisplayName", table: "Members", type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "");

        // The backfill uses the column added above, so it runs through EXEC.
        migrationBuilder.Sql("EXEC(N'UPDATE [Members] SET [DisplayName] = LEFT([Email], 100) WHERE LEN([DisplayName]) = 0')");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090800_CreateGenres")]
public class CreateGenres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Genres",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Genres", x => x.Id));

        migrationBuilder.InsertData(
            table: "Genres",
            columns: ["Id", "Name"],
            columnTypes: ["int", "nvarchar(50)"],
            values: new object[,] { { 1, "Fiction" }, { 2, "History" }, { 3, "Poetry" } });
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105090900_AddBookGenre")]
public class AddBookGenre : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "GenreId", table: "Books", type: "int", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Books_GenreId", table: "Books", column: "GenreId");
        migrationBuilder.AddForeignKey(
            name: "FK_Books_Genres_GenreId", table: "Books", column: "GenreId", principalTable: "Genres", principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091000_RenameAuthorBorn")]
public class RenameAuthorBorn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(name: "Born", table: "Authors", newName: "BirthDate");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091100_WidenBookTitle")]
public class WidenBookTitle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Title", table: "Books", type: "nvarchar(400)", maxLength: 400, nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(300)", oldMaxLength: 300);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091200_AddLoanMemberIndex")]
public class AddLoanMemberIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_Loans_MemberId_DueOn", table: "Loans", columns: ["MemberId", "DueOn"])
            .Annotation("SqlServer:Include", new[] { "ReturnedOn" });
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091300_CreateShelfNumbers")]
public class CreateShelfNumbers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence<int>(name: "ShelfNumbers", startValue: 100L, incrementBy: 10);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091400_CreateShelves")]
public class CreateShelves : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Shelves",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Number = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [ShelfNumbers]"),
                Label = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Shelves", x => x.Id));
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091500_CreateAvailableBooksView")]
public class CreateAvailableBooksView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            EXEC(N'CREATE VIEW [AvailableBooks] AS
            SELECT b.[Id], b.[Title]
            FROM [Books] b
            WHERE NOT EXISTS (SELECT 1 FROM [Loans] l WHERE l.[BookId] = b.[Id] AND l.[ReturnedOn] IS NULL)')
            """);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091600_DropMemberJoinedOnDefault")]
public class DropMemberJoinedOnDefault : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "JoinedOn", table: "Members", type: "datetime2", nullable: false,
            oldClrType: typeof(DateTime), oldType: "datetime2", oldDefaultValueSql: "SYSUTCDATETIME()");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091700_AddBookDescription")]
public class AddBookDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Description", table: "Books", type: "nvarchar(max)", nullable: true);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091800_AddGenreSortOrder")]
public class AddGenreSortOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(name: "SortOrder", table: "Genres", type: "smallint", nullable: false, defaultValue: (short)0);
        migrationBuilder.UpdateData(
            table: "Genres", keyColumns: ["Id"], keyColumnTypes: ["int"], keyValues: [3], columns: ["SortOrder"],
            columnTypes: ["smallint"], values: [(short)9]);
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105091900_AddShelfToBooks")]
public class AddShelfToBooks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ShelfId", table: "Books", type: "int", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Books_ShelfId", table: "Books", column: "ShelfId");
        migrationBuilder.AddForeignKey(
            name: "FK_Books_Shelves_ShelfId", table: "Books", column: "ShelfId", principalTable: "Shelves", principalColumn: "Id");
    }
}

[DbContext(typeof(LibraryContext))]
[Migration("20260105092000_AddLoanColumnstoreIndex")]
public class AddLoanColumnstoreIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("EXEC(N'CREATE NONCLUSTERED COLUMNSTORE INDEX [CS_Loans] ON [Loans] ([BookId], [MemberId], [DueOn])')");
    }
}
