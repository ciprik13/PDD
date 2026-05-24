using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "app",
                table: "Plants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_OwnerUserId",
                schema: "app",
                table: "Plants",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_AspNetUsers_OwnerUserId",
                schema: "app",
                table: "Plants",
                column: "OwnerUserId",
                principalSchema: "app",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plants_AspNetUsers_OwnerUserId",
                schema: "app",
                table: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_Plants_OwnerUserId",
                schema: "app",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "app",
                table: "Plants");
        }
    }
}
