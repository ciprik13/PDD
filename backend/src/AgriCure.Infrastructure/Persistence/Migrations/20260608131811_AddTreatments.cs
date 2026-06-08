using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Treatments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiseaseClass = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Dosage = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RepeatAfterDays = table.Column<int>(type: "integer", nullable: false),
                    PhiDays = table.Column<int>(type: "integer", nullable: false),
                    CostLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treatments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppliedTreatments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppliedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppliedTreatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppliedTreatments_AspNetUsers_AppliedByUserId",
                        column: x => x.AppliedByUserId,
                        principalSchema: "app",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppliedTreatments_Plants_PlantId",
                        column: x => x.PlantId,
                        principalSchema: "app",
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppliedTreatments_Treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalSchema: "app",
                        principalTable: "Treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppliedTreatments_AppliedAt",
                schema: "app",
                table: "AppliedTreatments",
                column: "AppliedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AppliedTreatments_AppliedByUserId",
                schema: "app",
                table: "AppliedTreatments",
                column: "AppliedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppliedTreatments_PlantId",
                schema: "app",
                table: "AppliedTreatments",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppliedTreatments_TreatmentId",
                schema: "app",
                table: "AppliedTreatments",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_DiseaseClass_Rank",
                schema: "app",
                table: "Treatments",
                columns: new[] { "DiseaseClass", "Rank" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppliedTreatments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Treatments",
                schema: "app");
        }
    }
}
