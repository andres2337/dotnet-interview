using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExternalId",
                table: "TodoList",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "TodoList",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "TodoList",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "TodoList",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "TodoList",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Synced");

            migrationBuilder.AddColumn<long>(
                name: "ExternalId",
                table: "TodoItem",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "TodoItem",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "TodoItem",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "TodoItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncStatus",
                table: "TodoItem",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Synced");

            migrationBuilder.CreateTable(
                name: "SyncRun",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    DeletedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRun", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_ExternalId",
                table: "TodoList",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_LastModifiedAtUtc",
                table: "TodoList",
                column: "LastModifiedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_SyncStatus",
                table: "TodoList",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItem_ExternalId",
                table: "TodoItem",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItem_LastModifiedAtUtc",
                table: "TodoItem",
                column: "LastModifiedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItem_SyncStatus",
                table: "TodoItem",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRun_StartedAtUtc",
                table: "SyncRun",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncRun");

            migrationBuilder.DropIndex(
                name: "IX_TodoList_ExternalId",
                table: "TodoList");

            migrationBuilder.DropIndex(
                name: "IX_TodoList_LastModifiedAtUtc",
                table: "TodoList");

            migrationBuilder.DropIndex(
                name: "IX_TodoList_SyncStatus",
                table: "TodoList");

            migrationBuilder.DropIndex(
                name: "IX_TodoItem_ExternalId",
                table: "TodoItem");

            migrationBuilder.DropIndex(
                name: "IX_TodoItem_LastModifiedAtUtc",
                table: "TodoItem");

            migrationBuilder.DropIndex(
                name: "IX_TodoItem_SyncStatus",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "TodoList");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "TodoList");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "TodoList");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "TodoList");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "TodoList");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "TodoItem");
        }
    }
}
