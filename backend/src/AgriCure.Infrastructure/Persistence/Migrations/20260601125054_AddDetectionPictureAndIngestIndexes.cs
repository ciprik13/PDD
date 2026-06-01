using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectionPictureAndIngestIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Detections_PlantId",
                schema: "app",
                table: "Detections");

            migrationBuilder.CreateTable(
                name: "DetectionPictures",
                schema: "app",
                columns: table => new
                {
                    DetectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PictureId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionPictures", x => new { x.DetectionId, x.PictureId });
                    table.ForeignKey(
                        name: "FK_DetectionPictures_Detections_DetectionId",
                        column: x => x.DetectionId,
                        principalSchema: "app",
                        principalTable: "Detections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetectionPictures_Pictures_PictureId",
                        column: x => x.PictureId,
                        principalSchema: "app",
                        principalTable: "Pictures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detections_PlantId_FrameId",
                schema: "app",
                table: "Detections",
                columns: new[] { "PlantId", "FrameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetectionPictures_DetectionId",
                schema: "app",
                table: "DetectionPictures",
                column: "DetectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetectionPictures_PictureId",
                schema: "app",
                table: "DetectionPictures",
                column: "PictureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectionPictures",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_Detections_PlantId_FrameId",
                schema: "app",
                table: "Detections");

            migrationBuilder.CreateIndex(
                name: "IX_Detections_PlantId",
                schema: "app",
                table: "Detections",
                column: "PlantId");
        }
    }
}
