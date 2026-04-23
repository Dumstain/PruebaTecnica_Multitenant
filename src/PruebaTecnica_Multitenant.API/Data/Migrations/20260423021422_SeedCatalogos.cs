using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PruebaTecnica_Multitenant.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "estado",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Pendiente" },
                    { 2, "En Progreso" },
                    { 3, "Completado" }
                });

            migrationBuilder.InsertData(
                table: "prioridad",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Baja" },
                    { 2, "Media" },
                    { 3, "Alta" }
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Miembro" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "estado",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "estado",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "estado",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "prioridad",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "prioridad",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "prioridad",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "rol",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "rol",
                keyColumn: "id",
                keyValue: 2);
        }
    }
}
