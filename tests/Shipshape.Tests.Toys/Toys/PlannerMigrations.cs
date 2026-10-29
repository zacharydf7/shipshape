using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Tests.Toys.Planner;

[DbContext(typeof(PlannerContext))]
[Migration("20260126090000_CreateProjects")]
public class CreateProjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Projects", x => x.Id));
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090100_CreateTasks")]
public class CreateTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ProjectId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Done = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tasks", x => x.Id);
                table.ForeignKey("FK_Tasks_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Tasks_ProjectId", table: "Tasks", column: "ProjectId");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090200_AddTaskPriority")]
public class AddTaskPriority : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Priority", table: "Tasks", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE [Tasks] SET [Priority] = 2 WHERE [Done] = 0;");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090300_CreateOpenTasksView")]
public class CreateOpenTasksView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE VIEW [OpenTasks] AS
            SELECT [Id], [Title], [Priority] FROM [Tasks] WHERE [Done] = 0;
            """);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090400_CreateTaskProcedures")]
public class CreateTaskProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Two batches, quotes inside strings, and a quote inside a comment.
        migrationBuilder.Sql("""
            CREATE PROCEDURE [CompleteTask] @id int AS
            -- don't touch tasks that are already done
            UPDATE [Tasks] SET [Done] = 1, [Title] = [Title] + N' (it''s done)' WHERE [Id] = @id AND [Done] = 0;
            GO
            CREATE PROCEDURE [ReopenTask] @id int AS
            UPDATE [Tasks] SET [Done] = 0, [Title] = REPLACE([Title], N' (it''s done)', N'') WHERE [Id] = @id;
            """);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090500_CreateTags")]
public class CreateTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Tags", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_Tags_Name", table: "Tags", column: "Name", unique: true);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090600_CreateTaskTags")]
public class CreateTaskTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TaskTags",
            columns: table => new
            {
                TaskId = table.Column<int>(type: "int", nullable: false),
                TagId = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskTags", x => new { x.TaskId, x.TagId });
                table.ForeignKey("FK_TaskTags_Tasks_TaskId", x => x.TaskId, "Tasks", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TaskTags_Tags_TagId", x => x.TagId, "Tags", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_TaskTags_TagId", table: "TaskTags", column: "TagId");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090700_AddTaskDueOn")]
public class AddTaskDueOn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(name: "DueOn", table: "Tasks", type: "date", nullable: true);
        migrationBuilder.Sql("UPDATE [Tasks] SET [DueOn] = CAST(SYSUTCDATETIME() AS date) WHERE [Done] = 0 AND [DueOn] IS NULL;");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090800_CreateComments")]
public class CreateComments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                TaskId = table.Column<int>(type: "int", nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PostedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.Id);
                table.ForeignKey("FK_Comments_Tasks_TaskId", x => x.TaskId, "Tasks", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Comments_TaskId", table: "Comments", column: "TaskId");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126090900_AddProjectArchived")]
public class AddProjectArchived : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "Archived", table: "Projects", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.CreateIndex(name: "IX_Projects_Archived", table: "Projects", column: "Archived", filter: "[Archived] = 0");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091000_WidenTaskTitle")]
public class WidenTaskTitle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Title", table: "Tasks", type: "nvarchar(300)", maxLength: 300, nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(200)", oldMaxLength: 200);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091100_AddPriorityCheck")]
public class AddPriorityCheck : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(name: "CK_Tasks_Priority", table: "Tasks", sql: "[Priority] BETWEEN 0 AND 5");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091200_CreateMilestones")]
public class CreateMilestones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Milestones",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ProjectId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DueOn = table.Column<DateOnly>(type: "date", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Milestones", x => x.Id);
                table.ForeignKey(
                    "FK_Milestones_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Milestones_ProjectId_DueOn", table: "Milestones", columns: ["ProjectId", "DueOn"]);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091300_AddTaskMilestone")]
public class AddTaskMilestone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "MilestoneId", table: "Tasks", type: "int", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Tasks_MilestoneId", table: "Tasks", column: "MilestoneId");
        migrationBuilder.AddForeignKey(
            name: "FK_Tasks_Milestones_MilestoneId", table: "Tasks", column: "MilestoneId", principalTable: "Milestones",
            principalColumn: "Id", onDelete: ReferentialAction.NoAction);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091400_AddTaskEstimate")]
public class AddTaskEstimate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "EstimateHours", table: "Tasks", type: "decimal(5,1)", precision: 5, scale: 1, nullable: true);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091500_CreateTaskSummaryFunction")]
public class CreateTaskSummaryFunction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE FUNCTION [OpenTaskCount](@projectId int) RETURNS int AS
            BEGIN
                RETURN (SELECT COUNT(*) FROM [Tasks] WHERE [ProjectId] = @projectId AND [Done] = 0);
            END
            """);
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091600_RenameCommentBody")]
public class RenameCommentBody : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(name: "Body", table: "Comments", newName: "Text");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091700_AddTagColor")]
public class AddTagColor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Color", table: "Tags", type: "char(7)", unicode: false, fixedLength: true, maxLength: 7, nullable: true);
        migrationBuilder.Sql("UPDATE [Tags] SET [Color] = '#336699' WHERE [Color] IS NULL;");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091800_AddDueOnIndex")]
public class AddDueOnIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_Tasks_DueOn", table: "Tasks", column: "DueOn", filter: "[DueOn] IS NOT NULL")
            .Annotation("SqlServer:Include", new[] { "Title", "Priority" });
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126091900_AddProjectCode")]
public class AddProjectCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Code", table: "Projects", type: "varchar(10)", unicode: false, maxLength: 10, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Projects_Code", table: "Projects", column: "Code", unique: true, filter: "[Code] IS NOT NULL");
    }
}

[DbContext(typeof(PlannerContext))]
[Migration("20260126092000_AddProjectOwner")]
public class AddProjectOwner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Owner", table: "Projects", type: "nvarchar(100)", maxLength: 100, nullable: true);

        // Starts and ends like a hand-wrapped EXEC, but the UPDATE between needs wrapping too.
        migrationBuilder.Sql("""
            EXEC(N'UPDATE [Projects] SET [Name] = LTRIM([Name])');
            UPDATE [Projects] SET [Owner] = N'unassigned' WHERE [Owner] IS NULL;
            EXEC(N'UPDATE [Projects] SET [Code] = UPPER([Code])')
            """);
    }
}
