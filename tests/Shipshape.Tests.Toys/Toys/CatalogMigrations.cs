using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Tests.Toys.Catalog;

[DbContext(typeof(CatalogContext))]
[Migration("20260112090000_CreateCategories")]
public class CreateCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "catalog");
        migrationBuilder.CreateTable(
            name: "Categories",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ParentId = table.Column<int>(type: "int", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
                table.ForeignKey(
                    "FK_Categories_Categories_ParentId", x => x.ParentId, principalSchema: "catalog", principalTable: "Categories",
                    principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Categories_ParentId", schema: "catalog", table: "Categories", column: "ParentId");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090100_CreateProducts")]
public class CreateProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Products",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Sku = table.Column<string>(
                    type: "varchar(32)", unicode: false, maxLength: 32, nullable: false, collation: "Latin1_General_BIN2"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey(
                    "FK_Products_Categories_CategoryId", x => x.CategoryId, principalSchema: "catalog", principalTable: "Categories",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Products_CategoryId", schema: "catalog", table: "Products", column: "CategoryId");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090200_AddProductSkuKey")]
public class AddProductSkuKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(name: "AK_Products_Sku", schema: "catalog", table: "Products", column: "Sku");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090300_CreatePrices")]
public class CreatePrices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Prices",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Prices", x => x.Id);
                table.ForeignKey(
                    "FK_Prices_Products_ProductId", x => x.ProductId, principalSchema: "catalog", principalTable: "Products",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "PricesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "catalog")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateIndex(name: "IX_Prices_ProductId", schema: "catalog", table: "Prices", column: "ProductId");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090400_AddProductWeight")]
public class AddProductWeight : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Weight", schema: "catalog", table: "Products", type: "decimal(9,3)", precision: 9, scale: 3, nullable: true);
        migrationBuilder.AddCheckConstraint(name: "CK_Products_Weight", schema: "catalog", table: "Products", sql: "[Weight] >= 0");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090500_CreateTags")]
public class CreateTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tags",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Tags", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ProductTags",
            schema: "catalog",
            columns: table => new
            {
                ProductId = table.Column<int>(type: "int", nullable: false),
                TagId = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductTags", x => new { x.ProductId, x.TagId });
                table.ForeignKey(
                    "FK_ProductTags_Products_ProductId", x => x.ProductId, principalSchema: "catalog", principalTable: "Products",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductTags_Tags_TagId", x => x.TagId, principalSchema: "catalog", principalTable: "Tags",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_ProductTags_TagId", schema: "catalog", table: "ProductTags", column: "TagId");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090600_SeedTags")]
public class SeedTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "catalog",
            table: "Tags",
            columns: ["Id", "Name"],
            columnTypes: ["int", "nvarchar(50)"],
            values: new object[,] { { 1, "New" }, { 2, "Sale" }, { 3, "Gift's choice" } });
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090700_WidenCategoryName")]
public class WidenCategoryName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name", schema: "catalog", table: "Categories", type: "nvarchar(150)", maxLength: 150, nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090800_AddProductNameIndex")]
public class AddProductNameIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_Products_Name", schema: "catalog", table: "Products", column: "Name");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112090900_RenameTagsToLabels")]
public class RenameTagsToLabels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(name: "Tags", schema: "catalog", newName: "Labels", newSchema: "catalog");
        migrationBuilder.RenameIndex(
            name: "IX_ProductTags_TagId", schema: "catalog", table: "ProductTags", newName: "IX_ProductTags_LabelId");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091000_AddProductIsDiscontinued")]
public class AddProductIsDiscontinued : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDiscontinued", schema: "catalog", table: "Products", type: "bit", nullable: false, defaultValue: false);
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091100_AddPriceValidFromIndex")]
public class AddPriceValidFromIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Prices_ProductId_ValidFrom", schema: "catalog", table: "Prices", columns: ["ProductId", "ValidFrom"],
            descending: [false, true]);
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091200_AddProductSlug")]
public class AddProductSlug : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Slug", schema: "catalog", table: "Products", type: "varchar(32)", unicode: false, maxLength: 32, nullable: true,
            computedColumnSql: "LOWER([Sku])", stored: true);
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091300_MarkUnnamedProductsDiscontinued")]
public class MarkUnnamedProductsDiscontinued : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Its own migration, so the column already exists when this batch compiles.
        migrationBuilder.Sql("UPDATE [catalog].[Products] SET [IsDiscontinued] = 1 WHERE LEN([Name]) = 0;");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091400_CreateProductImages")]
public class CreateProductImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductImages",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductImages", x => x.Id);
                table.ForeignKey(
                    "FK_ProductImages_Products_ProductId", x => x.ProductId, principalSchema: "catalog", principalTable: "Products",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_ProductImages_ProductId", schema: "catalog", table: "ProductImages", column: "ProductId");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091500_DropProductWeight")]
public class DropProductWeight : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_Products_Weight", schema: "catalog", table: "Products");
        migrationBuilder.DropColumn(name: "Weight", schema: "catalog", table: "Products");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091600_AddLabelColor")]
public class AddLabelColor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Color", schema: "catalog", table: "Labels", type: "char(7)", unicode: false, fixedLength: true, maxLength: 7,
            nullable: false, defaultValue: "#808080");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091700_RestrictProductCategoryDelete")]
public class RestrictProductCategoryDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Products_Categories_CategoryId", schema: "catalog", table: "Products");
        migrationBuilder.AddForeignKey(
            name: "FK_Products_Categories_CategoryId", schema: "catalog", table: "Products", column: "CategoryId",
            principalSchema: "catalog", principalTable: "Categories", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091800_AddDiscontinuedIndex")]
public class AddDiscontinuedIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Products_IsDiscontinued", schema: "catalog", table: "Products", column: "IsDiscontinued",
            filter: "[IsDiscontinued] = 1");
    }
}

[DbContext(typeof(CatalogContext))]
[Migration("20260112091900_AddCategorySortOrder")]
public class AddCategorySortOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SortOrder", schema: "catalog", table: "Categories", type: "int", nullable: false, defaultValue: 0);
    }
}
