using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDetections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plants",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Detections",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameId = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    BoundingBoxX = table.Column<double>(type: "double precision", nullable: false),
                    BoundingBoxY = table.Column<double>(type: "double precision", nullable: false),
                    BoundingBoxWidth = table.Column<double>(type: "double precision", nullable: false),
                    BoundingBoxHeight = table.Column<double>(type: "double precision", nullable: false),
                    BoundingBoxDepthMeters = table.Column<double>(type: "double precision", nullable: false),
                    BoundingBoxAffectedAreaPercent = table.Column<double>(type: "double precision", nullable: false),
                    InferenceMs = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceGatePassed = table.Column<bool>(type: "boolean", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PositionMeters = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detections_Plants_PlantId",
                        column: x => x.PlantId,
                        principalSchema: "app",
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassPrediction",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiseaseClass = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassPrediction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassPrediction_Detections_DetectionId",
                        column: x => x.DetectionId,
                        principalSchema: "app",
                        principalTable: "Detections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassPrediction_DetectionId_Rank",
                schema: "app",
                table: "ClassPrediction",
                columns: new[] { "DetectionId", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_Detections_PlantId",
                schema: "app",
                table: "Detections",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Detections_Row",
                schema: "app",
                table: "Detections",
                column: "Row");

            migrationBuilder.CreateIndex(
                name: "IX_Detections_Timestamp",
                schema: "app",
                table: "Detections",
                column: "Timestamp",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassPrediction",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Detections",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Plants",
                schema: "app");
        }
    }
}
