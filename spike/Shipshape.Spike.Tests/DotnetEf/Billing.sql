IF OBJECT_ID(N'[billing].[__BillingHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'billing') IS NULL EXEC(N'CREATE SCHEMA [billing];');
    CREATE TABLE [billing].[__BillingHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___BillingHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090000_CreateInvoiceNumbers'
)
BEGIN
    IF SCHEMA_ID(N'billing') IS NULL EXEC(N'CREATE SCHEMA [billing];');
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090000_CreateInvoiceNumbers'
)
BEGIN
    CREATE SEQUENCE [billing].[InvoiceNumbers] AS int START WITH 1000 INCREMENT BY 1 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090000_CreateInvoiceNumbers'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090000_CreateInvoiceNumbers', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090100_CreateCustomers'
)
BEGIN
    CREATE TABLE [billing].[Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090100_CreateCustomers'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090100_CreateCustomers', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090200_CreateInvoices'
)
BEGIN
    CREATE TABLE [billing].[Invoices] (
        [Id] int NOT NULL IDENTITY,
        [Number] int NOT NULL DEFAULT (NEXT VALUE FOR [billing].[InvoiceNumbers]),
        [CustomerId] int NOT NULL,
        [IssuedOn] date NOT NULL,
        [Status] tinyint NOT NULL DEFAULT CAST(0 AS tinyint),
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [billing].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090200_CreateInvoices'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [billing].[Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090200_CreateInvoices'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_Number] ON [billing].[Invoices] ([Number]) WHERE [Number] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090200_CreateInvoices'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090200_CreateInvoices', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090300_CreateInvoiceLines'
)
BEGIN
    CREATE TABLE [billing].[InvoiceLines] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(12,2) NOT NULL,
        [LineTotal] AS [Quantity] * [UnitPrice] PERSISTED,
        CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InvoiceLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [billing].[Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090300_CreateInvoiceLines'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [billing].[InvoiceLines] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090300_CreateInvoiceLines'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090300_CreateInvoiceLines', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090400_AddCustomerTaxId'
)
BEGIN
    ALTER TABLE [billing].[Customers] ADD [TaxId] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090400_AddCustomerTaxId'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_TaxId] ON [billing].[Customers] ([TaxId]) WHERE [TaxId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090400_AddCustomerTaxId'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090400_AddCustomerTaxId', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090500_CreatePayments'
)
BEGIN
    CREATE TABLE [billing].[Payments] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NULL,
        [Amount] decimal(12,2) NOT NULL,
        [PaidOn] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [billing].[Invoices] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090500_CreatePayments'
)
BEGIN
    CREATE INDEX [IX_Payments_InvoiceId] ON [billing].[Payments] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090500_CreatePayments'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090500_CreatePayments', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090600_CreateMarkPaidTrigger'
)
BEGIN
    EXEC(N'CREATE TRIGGER [billing].[TR_Payments_MarkPaid] ON [billing].[Payments] AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE i SET [Status] = 2 FROM [billing].[Invoices] i JOIN inserted p ON p.[InvoiceId] = i.[Id];
    END')
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090600_CreateMarkPaidTrigger'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090600_CreateMarkPaidTrigger', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090700_WidenCustomerEmail'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[billing].[Customers]') AND [c].[name] = N'Email');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [billing].[Customers] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [billing].[Customers] ALTER COLUMN [Email] nvarchar(320) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090700_WidenCustomerEmail'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090700_WidenCustomerEmail', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090800_CreatePaymentMethods'
)
BEGIN
    CREATE TABLE [billing].[PaymentMethods] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090800_CreatePaymentMethods'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[billing].[PaymentMethods]'))
        SET IDENTITY_INSERT [billing].[PaymentMethods] ON;
    EXEC(N'INSERT INTO [billing].[PaymentMethods] ([Id], [Name])
    VALUES (1, N''Card''),
    (2, N''Bank transfer''),
    (3, N''Cash'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[billing].[PaymentMethods]'))
        SET IDENTITY_INSERT [billing].[PaymentMethods] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090800_CreatePaymentMethods'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090800_CreatePaymentMethods', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090900_AddPaymentMethodToPayments'
)
BEGIN
    ALTER TABLE [billing].[Payments] ADD [PaymentMethodId] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090900_AddPaymentMethodToPayments'
)
BEGIN
    CREATE INDEX [IX_Payments_PaymentMethodId] ON [billing].[Payments] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090900_AddPaymentMethodToPayments'
)
BEGIN
    ALTER TABLE [billing].[Payments] ADD CONSTRAINT [FK_Payments_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [billing].[PaymentMethods] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119090900_AddPaymentMethodToPayments'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119090900_AddPaymentMethodToPayments', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091000_RenameIssuedOn'
)
BEGIN
    EXEC sp_rename N'[billing].[Invoices].[IssuedOn]', N'IssueDate', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091000_RenameIssuedOn'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091000_RenameIssuedOn', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091100_AddInvoiceDueOn'
)
BEGIN
    ALTER TABLE [billing].[Invoices] ADD [DueOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091100_AddInvoiceDueOn'
)
BEGIN
    EXEC(N'UPDATE [billing].[Invoices] SET [DueOn] = DATEADD(day, 30, [IssueDate]) WHERE [DueOn] IS NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091100_AddInvoiceDueOn'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091100_AddInvoiceDueOn', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091200_CreateMarkOverdueProcedure'
)
BEGIN
    EXEC(N'CREATE PROCEDURE [billing].[MarkOverdue] @asOf date AS
    UPDATE [billing].[Invoices] SET [Status] = 3 WHERE [Status] = 1 AND [DueOn] < @asOf;')
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091200_CreateMarkOverdueProcedure'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091200_CreateMarkOverdueProcedure', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091300_AddInvoiceCustomerDateIndex'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId_IssueDate] ON [billing].[Invoices] ([CustomerId], [IssueDate]) INCLUDE ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091300_AddInvoiceCustomerDateIndex'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091300_AddInvoiceCustomerDateIndex', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091400_AddCustomerCreatedOn'
)
BEGIN
    ALTER TABLE [billing].[Customers] ADD [CreatedOn] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091400_AddCustomerCreatedOn'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091400_AddCustomerCreatedOn', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091500_CreateCreditNotes'
)
BEGIN
    CREATE TABLE [billing].[CreditNotes] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [Reason] nvarchar(max) NULL,
        CONSTRAINT [PK_CreditNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditNotes_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_CreditNotes_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [billing].[Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091500_CreateCreditNotes'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_InvoiceId] ON [billing].[CreditNotes] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091500_CreateCreditNotes'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091500_CreateCreditNotes', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091600_AddPaymentReference'
)
BEGIN
    ALTER TABLE [billing].[Payments] ADD [Reference] varchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091600_AddPaymentReference'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091600_AddPaymentReference', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091700_AddInvoiceNotes'
)
BEGIN
    ALTER TABLE [billing].[Invoices] ADD [Notes] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091700_AddInvoiceNotes'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091700_AddInvoiceNotes', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091800_AddCustomerCountryIndex'
)
BEGIN
    CREATE INDEX [IX_Customers_CountryCode] ON [billing].[Customers] ([CountryCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091800_AddCustomerCountryIndex'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091800_AddCustomerCountryIndex', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091900_RenamePaymentMethodIndex'
)
BEGIN
    EXEC sp_rename N'[billing].[Payments].[IX_Payments_PaymentMethodId]', N'IX_Payments_MethodId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [billing].[__BillingHistory]
    WHERE [MigrationId] = N'20260119091900_RenamePaymentMethodIndex'
)
BEGIN
    INSERT INTO [billing].[__BillingHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119091900_RenamePaymentMethodIndex', N'10.0.14');
END;

COMMIT;
GO

