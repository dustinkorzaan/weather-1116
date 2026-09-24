using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "City",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeonameId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Admin1Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Admin1Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FeatureCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Population = table.Column<long>(type: "bigint", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GeoPoint = table.Column<Point>(type: "geography", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_GeonameId",
                schema: "dbo",
                table: "City",
                column: "GeonameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Population",
                schema: "dbo",
                table: "City",
                column: "Population",
                descending: new bool[0]);

            // EF Core cannot declare spatial indexes; GetCitiesHandler's IsWithinDistance filter uses this one.
            migrationBuilder.Sql("CREATE SPATIAL INDEX IX_City_GeoPoint ON dbo.City(GeoPoint);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "City",
                schema: "dbo");
        }
    }
}
