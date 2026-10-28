IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090000_CreateProjects'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090000_CreateProjects'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090000_CreateProjects', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090100_CreateTasks'
)
BEGIN
    CREATE TABLE [Tasks] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Done] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tasks_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090100_CreateTasks'
)
BEGIN
    CREATE INDEX [IX_Tasks_ProjectId] ON [Tasks] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090100_CreateTasks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090100_CreateTasks', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090200_AddTaskPriority'
)
BEGIN
    ALTER TABLE [Tasks] ADD [Priority] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090200_AddTaskPriority'
)
BEGIN
    UPDATE [Tasks] SET [Priority] = 2 WHERE [Done] = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090200_AddTaskPriority'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090200_AddTaskPriority', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090300_CreateOpenTasksView'
)
BEGIN
    CREATE VIEW [OpenTasks] AS
    SELECT [Id], [Title], [Priority] FROM [Tasks] WHERE [Done] = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090300_CreateOpenTasksView'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090300_CreateOpenTasksView', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090400_CreateTaskProcedures'
)
BEGIN
    CREATE PROCEDURE [CompleteTask] @id int AS
    -- don't touch tasks that are already done
    UPDATE [Tasks] SET [Done] = 1, [Title] = [Title] + N' (it''s done)' WHERE [Id] = @id AND [Done] = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090400_CreateTaskProcedures'
)
BEGIN
    CREATE PROCEDURE [ReopenTask] @id int AS
    UPDATE [Tasks] SET [Done] = 0, [Title] = REPLACE([Title], N' (it''s done)', N'') WHERE [Id] = @id;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090400_CreateTaskProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090400_CreateTaskProcedures', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090500_CreateTags'
)
BEGIN
    CREATE TABLE [Tags] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090500_CreateTags'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tags_Name] ON [Tags] ([Name]) WHERE [Name] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090500_CreateTags'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090500_CreateTags', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090600_CreateTaskTags'
)
BEGIN
    CREATE TABLE [TaskTags] (
        [TaskId] int NOT NULL,
        [TagId] int NOT NULL,
        CONSTRAINT [PK_TaskTags] PRIMARY KEY ([TaskId], [TagId]),
        CONSTRAINT [FK_TaskTags_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090600_CreateTaskTags'
)
BEGIN
    CREATE INDEX [IX_TaskTags_TagId] ON [TaskTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090600_CreateTaskTags'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090600_CreateTaskTags', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090700_AddTaskDueOn'
)
BEGIN
    ALTER TABLE [Tasks] ADD [DueOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090700_AddTaskDueOn'
)
BEGIN
    UPDATE [Tasks] SET [DueOn] = CAST(SYSUTCDATETIME() AS date) WHERE [Done] = 0 AND [DueOn] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090700_AddTaskDueOn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090700_AddTaskDueOn', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090800_CreateComments'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] bigint NOT NULL IDENTITY,
        [TaskId] int NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [PostedOn] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090800_CreateComments'
)
BEGIN
    CREATE INDEX [IX_Comments_TaskId] ON [Comments] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090800_CreateComments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090800_CreateComments', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090900_AddProjectArchived'
)
BEGIN
    ALTER TABLE [Projects] ADD [Archived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090900_AddProjectArchived'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Projects_Archived] ON [Projects] ([Archived]) WHERE [Archived] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126090900_AddProjectArchived'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126090900_AddProjectArchived', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091000_WidenTaskTitle'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'Title');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Tasks] ALTER COLUMN [Title] nvarchar(300) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091000_WidenTaskTitle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091000_WidenTaskTitle', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091100_AddPriorityCheck'
)
BEGIN
    EXEC(N'ALTER TABLE [Tasks] ADD CONSTRAINT [CK_Tasks_Priority] CHECK ([Priority] BETWEEN 0 AND 5)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091100_AddPriorityCheck'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091100_AddPriorityCheck', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091200_CreateMilestones'
)
BEGIN
    CREATE TABLE [Milestones] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DueOn] date NOT NULL,
        CONSTRAINT [PK_Milestones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Milestones_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091200_CreateMilestones'
)
BEGIN
    CREATE INDEX [IX_Milestones_ProjectId_DueOn] ON [Milestones] ([ProjectId], [DueOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091200_CreateMilestones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091200_CreateMilestones', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091300_AddTaskMilestone'
)
BEGIN
    ALTER TABLE [Tasks] ADD [MilestoneId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091300_AddTaskMilestone'
)
BEGIN
    CREATE INDEX [IX_Tasks_MilestoneId] ON [Tasks] ([MilestoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091300_AddTaskMilestone'
)
BEGIN
    ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Milestones_MilestoneId] FOREIGN KEY ([MilestoneId]) REFERENCES [Milestones] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091300_AddTaskMilestone'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091300_AddTaskMilestone', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091400_AddTaskEstimate'
)
BEGIN
    ALTER TABLE [Tasks] ADD [EstimateHours] decimal(5,1) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091400_AddTaskEstimate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091400_AddTaskEstimate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091500_CreateTaskSummaryFunction'
)
BEGIN
    CREATE FUNCTION [OpenTaskCount](@projectId int) RETURNS int AS
    BEGIN
        RETURN (SELECT COUNT(*) FROM [Tasks] WHERE [ProjectId] = @projectId AND [Done] = 0);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091500_CreateTaskSummaryFunction'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091500_CreateTaskSummaryFunction', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091600_RenameCommentBody'
)
BEGIN
    EXEC sp_rename N'[Comments].[Body]', N'Text', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091600_RenameCommentBody'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091600_RenameCommentBody', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091700_AddTagColor'
)
BEGIN
    ALTER TABLE [Tags] ADD [Color] char(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091700_AddTagColor'
)
BEGIN
    UPDATE [Tags] SET [Color] = '#336699' WHERE [Color] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091700_AddTagColor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091700_AddTagColor', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091800_AddDueOnIndex'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Tasks_DueOn] ON [Tasks] ([DueOn]) INCLUDE ([Title], [Priority]) WHERE [DueOn] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091800_AddDueOnIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091800_AddDueOnIndex', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091900_AddProjectCode'
)
BEGIN
    ALTER TABLE [Projects] ADD [Code] varchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091900_AddProjectCode'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Projects_Code] ON [Projects] ([Code]) WHERE [Code] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126091900_AddProjectCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126091900_AddProjectCode', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126092000_AddProjectOwner'
)
BEGIN
    ALTER TABLE [Projects] ADD [Owner] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126092000_AddProjectOwner'
)
BEGIN
    EXEC(N'UPDATE [Projects] SET [Name] = LTRIM([Name])');
    UPDATE [Projects] SET [Owner] = N'unassigned' WHERE [Owner] IS NULL;
    EXEC(N'UPDATE [Projects] SET [Code] = UPPER([Code])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260126092000_AddProjectOwner'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126092000_AddProjectOwner', N'10.0.14');
END;

COMMIT;
GO

