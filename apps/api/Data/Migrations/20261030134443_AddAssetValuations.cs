using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AssetPortfolioOps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetValuations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValuationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsDemonstrationData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetValuations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AssetValuations",
                columns: new[] { "Id", "AssetId", "IsDemonstrationData", "Price", "Source", "ValuationDate" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-4000-8000-000000000001"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 9750m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 10, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000002"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10050m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 11, 30) },
                    { new Guid("00000001-0000-4000-8000-000000000003"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 12, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000004"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 1, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000005"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10550m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 2, 28) },
                    { new Guid("00000001-0000-4000-8000-000000000006"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10550m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 3, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000007"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10450m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 4, 30) },
                    { new Guid("00000001-0000-4000-8000-000000000008"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 5, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000009"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 6, 30) },
                    { new Guid("00000001-0000-4000-8000-00000000000a"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10450m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 7, 31) },
                    { new Guid("00000001-0000-4000-8000-00000000000b"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10600m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 8, 31) },
                    { new Guid("00000001-0000-4000-8000-00000000000c"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 10900m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 9, 30) },
                    { new Guid("00000001-0000-4000-8000-00000000000d"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11200m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 10, 31) },
                    { new Guid("00000001-0000-4000-8000-00000000000e"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 11, 30) },
                    { new Guid("00000001-0000-4000-8000-00000000000f"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11750m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 12, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000010"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11900m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 1, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000011"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 12000m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 2, 28) },
                    { new Guid("00000001-0000-4000-8000-000000000012"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11950m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 3, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000013"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11900m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 4, 30) },
                    { new Guid("00000001-0000-4000-8000-000000000014"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11850m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 5, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000015"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11800m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 6, 30) },
                    { new Guid("00000001-0000-4000-8000-000000000016"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 11900m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 7, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000017"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 12050m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 8, 31) },
                    { new Guid("00000001-0000-4000-8000-000000000018"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, 12500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 9, 30) },
                    { new Guid("00000002-0000-4000-8000-000000000001"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 7850m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 10, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000002"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8100m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 11, 30) },
                    { new Guid("00000002-0000-4000-8000-000000000003"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 12, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000004"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 1, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000005"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 2, 28) },
                    { new Guid("00000002-0000-4000-8000-000000000006"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 3, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000007"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8100m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 4, 30) },
                    { new Guid("00000002-0000-4000-8000-000000000008"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 7950m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 5, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000009"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 7800m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 6, 30) },
                    { new Guid("00000002-0000-4000-8000-00000000000a"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 7800m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 7, 31) },
                    { new Guid("00000002-0000-4000-8000-00000000000b"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 7900m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 8, 31) },
                    { new Guid("00000002-0000-4000-8000-00000000000c"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8100m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 9, 30) },
                    { new Guid("00000002-0000-4000-8000-00000000000d"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 10, 31) },
                    { new Guid("00000002-0000-4000-8000-00000000000e"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8650m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 11, 30) },
                    { new Guid("00000002-0000-4000-8000-00000000000f"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8850m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 12, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000010"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8950m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 1, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000011"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8950m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 2, 28) },
                    { new Guid("00000002-0000-4000-8000-000000000012"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8850m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 3, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000013"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8650m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 4, 30) },
                    { new Guid("00000002-0000-4000-8000-000000000014"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 5, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000015"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 6, 30) },
                    { new Guid("00000002-0000-4000-8000-000000000016"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8350m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 7, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000017"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8450m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 8, 31) },
                    { new Guid("00000002-0000-4000-8000-000000000018"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, 8900m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 9, 30) },
                    { new Guid("00000003-0000-4000-8000-000000000001"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4450m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 10, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000002"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 11, 30) },
                    { new Guid("00000003-0000-4000-8000-000000000003"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2024, 12, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000004"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 1, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000005"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4500m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 2, 28) },
                    { new Guid("00000003-0000-4000-8000-000000000006"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4450m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 3, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000007"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4400m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 4, 30) },
                    { new Guid("00000003-0000-4000-8000-000000000008"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4350m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 5, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000009"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 6, 30) },
                    { new Guid("00000003-0000-4000-8000-00000000000a"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4250m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 7, 31) },
                    { new Guid("00000003-0000-4000-8000-00000000000b"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4250m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 8, 31) },
                    { new Guid("00000003-0000-4000-8000-00000000000c"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 9, 30) },
                    { new Guid("00000003-0000-4000-8000-00000000000d"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 10, 31) },
                    { new Guid("00000003-0000-4000-8000-00000000000e"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4350m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 11, 30) },
                    { new Guid("00000003-0000-4000-8000-00000000000f"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4350m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2025, 12, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000010"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4350m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 1, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000011"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4350m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 2, 28) },
                    { new Guid("00000003-0000-4000-8000-000000000012"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4300m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 3, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000013"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4250m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 4, 30) },
                    { new Guid("00000003-0000-4000-8000-000000000014"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4200m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 5, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000015"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4150m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 6, 30) },
                    { new Guid("00000003-0000-4000-8000-000000000016"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4150m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 7, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000017"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4150m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 8, 31) },
                    { new Guid("00000003-0000-4000-8000-000000000018"), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), true, 4200m, "Demonstration data (synthetic series, not market prices)", new DateOnly(2026, 9, 30) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetValuations_AssetId_ValuationDate",
                table: "AssetValuations",
                columns: new[] { "AssetId", "ValuationDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetValuations");
        }
    }
}
