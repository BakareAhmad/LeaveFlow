using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveFlow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExpenseManagerReviewAndPerformanceGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseClaims_Users_ManagerReviewedBy",
                table: "ExpenseClaims");

            migrationBuilder.DropTable(
                name: "PerformanceGoals");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseClaims_ManagerReviewedBy",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ManagerComment",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ManagerReviewedAt",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ManagerReviewedBy",
                table: "ExpenseClaims");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagerComment",
                table: "ExpenseClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerReviewedAt",
                table: "ExpenseClaims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerReviewedBy",
                table: "ExpenseClaims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerformanceGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformanceReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ManagerComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceGoals_PerformanceReviews_PerformanceReviewId",
                        column: x => x.PerformanceReviewId,
                        principalTable: "PerformanceReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_ManagerReviewedBy",
                table: "ExpenseClaims",
                column: "ManagerReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceGoals_PerformanceReviewId",
                table: "PerformanceGoals",
                column: "PerformanceReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseClaims_Users_ManagerReviewedBy",
                table: "ExpenseClaims",
                column: "ManagerReviewedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
