using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Data.Migrations
{
    /// <summary>
    /// Moves every table to EF Core's plural naming (dbo.AgentActivities, dbo.Users, dbo.Cities) and
    /// renames dbo.UserPin to dbo.UserCities. Everything is renamed in place -- tables, indexes, primary
    /// keys, and the foreign key -- so existing rows survive. Primary keys are renamed with sp_rename
    /// rather than dropped and re-added: SQL Server refuses to drop PK_City while the spatial index
    /// IX_City_GeoPoint (raw SQL in AddCity, not in the EF model) exists.
    /// </summary>
    public partial class PluralizeTablesAndRenameUserCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AgentActivity",
                schema: "dbo",
                newName: "AgentActivities",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "dbo",
                newName: "Users",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserPin",
                schema: "dbo",
                newName: "UserCities",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "City",
                schema: "dbo",
                newName: "Cities",
                newSchema: "dbo");

            migrationBuilder.RenameIndex(
                name: "IX_UserPin_UserId",
                schema: "dbo",
                table: "UserCities",
                newName: "IX_UserCities_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_City_GeonameId",
                schema: "dbo",
                table: "Cities",
                newName: "IX_Cities_GeonameId");

            migrationBuilder.RenameIndex(
                name: "IX_City_Population",
                schema: "dbo",
                table: "Cities",
                newName: "IX_Cities_Population");

            migrationBuilder.Sql("EXEC sp_rename N'dbo.Cities.IX_City_GeoPoint', N'IX_Cities_GeoPoint', N'INDEX';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_AgentActivity', N'PK_AgentActivities', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_User', N'PK_Users', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_UserPin', N'PK_UserCities', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_City', N'PK_Cities', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.FK_UserPin_User_UserId', N'FK_UserCities_Users_UserId', N'OBJECT';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AgentActivities",
                schema: "dbo",
                newName: "AgentActivity",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "dbo",
                newName: "User",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserCities",
                schema: "dbo",
                newName: "UserPin",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Cities",
                schema: "dbo",
                newName: "City",
                newSchema: "dbo");

            migrationBuilder.RenameIndex(
                name: "IX_UserCities_UserId",
                schema: "dbo",
                table: "UserPin",
                newName: "IX_UserPin_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_GeonameId",
                schema: "dbo",
                table: "City",
                newName: "IX_City_GeonameId");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_Population",
                schema: "dbo",
                table: "City",
                newName: "IX_City_Population");

            migrationBuilder.Sql("EXEC sp_rename N'dbo.City.IX_Cities_GeoPoint', N'IX_City_GeoPoint', N'INDEX';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_AgentActivities', N'PK_AgentActivity', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_Users', N'PK_User', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_UserCities', N'PK_UserPin', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.PK_Cities', N'PK_City', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'dbo.FK_UserCities_Users_UserId', N'FK_UserPin_User_UserId', N'OBJECT';");
        }
    }
}
