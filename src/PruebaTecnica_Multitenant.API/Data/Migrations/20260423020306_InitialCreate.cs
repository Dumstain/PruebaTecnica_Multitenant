using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PruebaTecnica_Multitenant.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estado",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prioridad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prioridad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizaciones_usuarios",
                columns: table => new
                {
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizaciones_usuarios", x => new { x.organizacion_id, x.usuario_id });
                    table.ForeignKey(
                        name: "FK_organizaciones_usuarios_organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organizaciones_usuarios_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organizaciones_usuarios_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tarea",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    estado_id = table.Column<int>(type: "integer", nullable: false),
                    prioridad_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_de_creacion = table.Column<DateTime>(type: "timestamp", nullable: false),
                    fecha_limite = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarea", x => x.id);
                    table.ForeignKey(
                        name: "FK_tarea_estado_estado_id",
                        column: x => x.estado_id,
                        principalTable: "estado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tarea_organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tarea_prioridad_prioridad_id",
                        column: x => x.prioridad_id,
                        principalTable: "prioridad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tarea_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organizaciones_usuarios_rol_id",
                table: "organizaciones_usuarios",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "IX_organizaciones_usuarios_usuario_id",
                table: "organizaciones_usuarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_tarea_estado_id",
                table: "tarea",
                column: "estado_id");

            migrationBuilder.CreateIndex(
                name: "IX_tarea_organizacion_id",
                table: "tarea",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_tarea_prioridad_id",
                table: "tarea",
                column: "prioridad_id");

            migrationBuilder.CreateIndex(
                name: "IX_tarea_usuario_id",
                table: "tarea",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizaciones_usuarios");

            migrationBuilder.DropTable(
                name: "tarea");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "estado");

            migrationBuilder.DropTable(
                name: "organizacion");

            migrationBuilder.DropTable(
                name: "prioridad");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
