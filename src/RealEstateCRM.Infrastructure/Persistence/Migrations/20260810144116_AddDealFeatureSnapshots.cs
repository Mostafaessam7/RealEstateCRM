using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDealFeatureSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "FeatureSnapshotBudgetFit",
                table: "Deals",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FeatureSnapshotLocationMatch",
                table: "Deals",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FeatureSnapshotPriceToBudgetRatio",
                table: "Deals",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FeatureSnapshotPropertyTypeMatch",
                table: "Deals",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatureSnapshotBudgetFit",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "FeatureSnapshotLocationMatch",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "FeatureSnapshotPriceToBudgetRatio",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "FeatureSnapshotPropertyTypeMatch",
                table: "Deals");
        }
    }
}
