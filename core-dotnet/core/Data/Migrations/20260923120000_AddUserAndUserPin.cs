using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndUserPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPin",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPin_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPin_UserId",
                schema: "dbo",
                table: "UserPin",
                column: "UserId");

            // Seed anonymous user with empty strings
            migrationBuilder.InsertData(
                schema: "dbo",
                table: "User",
                columns: new[] { "Id", "FirstName", "LastName", "Email" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000000"), "", "", "" });

            // Seed 4 default pins for anonymous user
            migrationBuilder.InsertData(
                schema: "dbo",
                table: "UserPin",
                columns: new[] { "Id", "UserId", "Latitude", "Longitude", "LocationName" },
                values: new object[,]
                {
                    { new Guid("59e2459a-b25d-44a7-bcb0-2a4f2e444272"), new Guid("00000000-0000-0000-0000-000000000000"), 40.7128, -74.006, "New York, NY" },
                    { new Guid("329735f1-cfc0-42b4-a48f-0d41677145e8"), new Guid("00000000-0000-0000-0000-000000000000"), 43.6532, -79.3832, "Toronto, ON" },
                    { new Guid("9daab691-7885-400f-8aed-5e21a63f9a7a"), new Guid("00000000-0000-0000-0000-000000000000"), 33.749, -84.388, "Atlanta, GA" },
                    { new Guid("04f5d22f-ca31-4d29-ac9e-a1c4f0127ed1"), new Guid("00000000-0000-0000-0000-000000000000"), 35.2271, -80.8431, "Charlotte, NC" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPin",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "User",
                schema: "dbo");
        }
    }
}
