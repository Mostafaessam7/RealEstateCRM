using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhatsAppTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_AspNetUsers_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_WhatsAppTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "WhatsAppTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CompanyId_LeadId",
                table: "WhatsAppMessages",
                columns: new[] { "CompanyId", "LeadId" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_LeadId",
                table: "WhatsAppMessages",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_SentByUserId",
                table: "WhatsAppMessages",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_TemplateId",
                table: "WhatsAppMessages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_CompanyId_IsActive",
                table: "WhatsAppTemplates",
                columns: new[] { "CompanyId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppMessages");

            migrationBuilder.DropTable(
                name: "WhatsAppTemplates");
        }
    }
}
