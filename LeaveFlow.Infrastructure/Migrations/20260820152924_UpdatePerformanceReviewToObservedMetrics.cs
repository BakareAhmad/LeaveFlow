using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveFlow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePerformanceReviewToObservedMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "EmployeeComments",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "EmployeeSubmittedAt",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "PerformanceReviews");

            migrationBuilder.CreateTable(
                name: "DepartmentReviewReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentReviewReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentReviewReports_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentReviewReports_PerformanceReviewCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PerformanceReviewCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentReviewReports_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "DeletedAt", "Description", "IsDeleted", "MaximumDays", "Name" },
                values: new object[] { new Guid("88888888-8888-8888-8888-888888888888"), null, "Standard annual paid leave entitlement.", false, 30, "Annual Leave" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentReviewReports_CycleId_DepartmentId",
                table: "DepartmentReviewReports",
                columns: new[] { "CycleId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentReviewReports_DepartmentId",
                table: "DepartmentReviewReports",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentReviewReports_ManagerUserId",
                table: "DepartmentReviewReports",
                column: "ManagerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentReviewReports");

            migrationBuilder.DeleteData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "PerformanceReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeComments",
                table: "PerformanceReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeSubmittedAt",
                table: "PerformanceReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "PerformanceReviews",
                type: "int",
                nullable: true);
        }
    }
}
