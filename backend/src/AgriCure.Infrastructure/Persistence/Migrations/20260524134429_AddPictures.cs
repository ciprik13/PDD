using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPictures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pictures",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    SeoFilename = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AltAttribute = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TitleAttribute = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsNew = table.Column<bool>(type: "boolean", nullable: false),
                    VirtualPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pictures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_VirtualPath",
                schema: "app",
                table: "Pictures",
                column: "VirtualPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pictures",
                schema: "app");
        }
    }
}
