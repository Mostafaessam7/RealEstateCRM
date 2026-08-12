using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RealEstateCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxLeads = table.Column<int>(type: "int", nullable: false),
                    MaxUnits = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "MaxLeads", "MaxUnits", "MaxUsers", "MonthlyPrice", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), "free", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 100, 25, 3, 0m, "Free", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000002"), "starter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1000, 200, 10, 49m, "Starter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000003"), "pro", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 10000, 2000, 30, 149m, "Pro", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000004"), "enterprise", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1000000, 1000000, 1000, 499m, "Enterprise", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_CompanyId",
                table: "CompanySubscriptions",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_PlanId",
                table: "CompanySubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
