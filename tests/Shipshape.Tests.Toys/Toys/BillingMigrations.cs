using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Tests.Toys.Billing;

[DbContext(typeof(BillingContext))]
[Migration("20260119090000_CreateInvoiceNumbers")]
public class CreateInvoiceNumbers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "billing");
        migrationBuilder.CreateSequence<int>(name: "InvoiceNumbers", schema: "billing", startValue: 1000L);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090100_CreateCustomers")]
public class CreateCustomers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            schema: "billing",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090200_CreateInvoices")]
public class CreateInvoices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Invoices",
            schema: "billing",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Number = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [billing].[InvoiceNumbers]"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey(
                    "FK_Invoices_Customers_CustomerId", x => x.CustomerId, principalSchema: "billing", principalTable: "Customers",
                    principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Invoices_CustomerId", schema: "billing", table: "Invoices", column: "CustomerId");
        migrationBuilder.CreateIndex(name: "IX_Invoices_Number", schema: "billing", table: "Invoices", column: "Number", unique: true);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090300_CreateInvoiceLines")]
public class CreateInvoiceLines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InvoiceLines",
            schema: "billing",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                InvoiceId = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                LineTotal = table.Column<decimal>(
                    type: "decimal(23,2)", nullable: true, computedColumnSql: "[Quantity] * [UnitPrice]", stored: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                table.ForeignKey(
                    "FK_InvoiceLines_Invoices_InvoiceId", x => x.InvoiceId, principalSchema: "billing", principalTable: "Invoices",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_InvoiceLines_Quantity", "[Quantity] > 0");
            });

        migrationBuilder.CreateIndex(name: "IX_InvoiceLines_InvoiceId", schema: "billing", table: "InvoiceLines", column: "InvoiceId");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090400_AddCustomerTaxId")]
public class AddCustomerTaxId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TaxId", schema: "billing", table: "Customers", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Customers_TaxId", schema: "billing", table: "Customers", column: "TaxId", unique: true,
            filter: "[TaxId] IS NOT NULL");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090500_CreatePayments")]
public class CreatePayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Payments",
            schema: "billing",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                InvoiceId = table.Column<int>(type: "int", nullable: true),
                Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                PaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    "FK_Payments_Invoices_InvoiceId", x => x.InvoiceId, principalSchema: "billing", principalTable: "Invoices",
                    principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_Payments_InvoiceId", schema: "billing", table: "Payments", column: "InvoiceId");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090600_CreateMarkPaidTrigger")]
public class CreateMarkPaidTrigger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER [billing].[TR_Payments_MarkPaid] ON [billing].[Payments] AFTER INSERT AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE i SET [Status] = 2 FROM [billing].[Invoices] i JOIN inserted p ON p.[InvoiceId] = i.[Id];
            END')
            """);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090700_WidenCustomerEmail")]
public class WidenCustomerEmail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Email", schema: "billing", table: "Customers", type: "nvarchar(320)", maxLength: 320, nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(200)", oldMaxLength: 200);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090800_CreatePaymentMethods")]
public class CreatePaymentMethods : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentMethods",
            schema: "billing",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_PaymentMethods", x => x.Id));

        migrationBuilder.InsertData(
            schema: "billing",
            table: "PaymentMethods",
            columns: ["Id", "Name"],
            columnTypes: ["int", "nvarchar(50)"],
            values: new object[,] { { 1, "Card" }, { 2, "Bank transfer" }, { 3, "Cash" } });
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119090900_AddPaymentMethodToPayments")]
public class AddPaymentMethodToPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PaymentMethodId", schema: "billing", table: "Payments", type: "int", nullable: false, defaultValue: 1);
        migrationBuilder.CreateIndex(name: "IX_Payments_PaymentMethodId", schema: "billing", table: "Payments", column: "PaymentMethodId");
        migrationBuilder.AddForeignKey(
            name: "FK_Payments_PaymentMethods_PaymentMethodId", schema: "billing", table: "Payments", column: "PaymentMethodId",
            principalSchema: "billing", principalTable: "PaymentMethods", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091000_RenameIssuedOn")]
public class RenameIssuedOn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(name: "IssuedOn", schema: "billing", table: "Invoices", newName: "IssueDate");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091100_AddInvoiceDueOn")]
public class AddInvoiceDueOn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(name: "DueOn", schema: "billing", table: "Invoices", type: "date", nullable: true);

        // Uses the column added above, so it runs through EXEC.
        migrationBuilder.Sql("EXEC(N'UPDATE [billing].[Invoices] SET [DueOn] = DATEADD(day, 30, [IssueDate]) WHERE [DueOn] IS NULL')");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091200_CreateMarkOverdueProcedure")]
public class CreateMarkOverdueProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            EXEC(N'CREATE PROCEDURE [billing].[MarkOverdue] @asOf date AS
            UPDATE [billing].[Invoices] SET [Status] = 3 WHERE [Status] = 1 AND [DueOn] < @asOf;')
            """);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091300_AddInvoiceCustomerDateIndex")]
public class AddInvoiceCustomerDateIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId_IssueDate", schema: "billing", table: "Invoices", columns: ["CustomerId", "IssueDate"])
            .Annotation("SqlServer:Include", new[] { "Status" });
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091400_AddCustomerCreatedOn")]
public class AddCustomerCreatedOn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedOn", schema: "billing", table: "Customers", type: "datetime2(3)", precision: 3, nullable: false,
            defaultValueSql: "SYSUTCDATETIME()");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091500_CreateCreditNotes")]
public class CreateCreditNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CreditNotes",
            schema: "billing",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                InvoiceId = table.Column<int>(type: "int", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CreditNotes", x => x.Id);
                table.ForeignKey(
                    "FK_CreditNotes_Invoices_InvoiceId", x => x.InvoiceId, principalSchema: "billing", principalTable: "Invoices",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_CreditNotes_Amount", "[Amount] > 0");
            });

        migrationBuilder.CreateIndex(name: "IX_CreditNotes_InvoiceId", schema: "billing", table: "CreditNotes", column: "InvoiceId");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091600_AddPaymentReference")]
public class AddPaymentReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Reference", schema: "billing", table: "Payments", type: "varchar(64)", unicode: false, maxLength: 64,
            nullable: true);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091700_AddInvoiceNotes")]
public class AddInvoiceNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Notes", schema: "billing", table: "Invoices", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091800_AddCustomerCountryIndex")]
public class AddCustomerCountryIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_Customers_CountryCode", schema: "billing", table: "Customers", column: "CountryCode");
    }
}

[DbContext(typeof(BillingContext))]
[Migration("20260119091900_RenamePaymentMethodIndex")]
public class RenamePaymentMethodIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameIndex(
            name: "IX_Payments_PaymentMethodId", schema: "billing", table: "Payments", newName: "IX_Payments_MethodId");
    }
}
