using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExposePredictionsDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassPrediction_Detections_DetectionId",
                schema: "app",
                table: "ClassPrediction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassPrediction",
                schema: "app",
                table: "ClassPrediction");

            migrationBuilder.RenameTable(
                name: "ClassPrediction",
                schema: "app",
                newName: "Predictions",
                newSchema: "app");

            migrationBuilder.RenameIndex(
                name: "IX_ClassPrediction_DetectionId_Rank",
                schema: "app",
                table: "Predictions",
                newName: "IX_Predictions_DetectionId_Rank");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Predictions",
                schema: "app",
                table: "Predictions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Detections_DetectionId",
                schema: "app",
                table: "Predictions",
                column: "DetectionId",
                principalSchema: "app",
                principalTable: "Detections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Detections_DetectionId",
                schema: "app",
                table: "Predictions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Predictions",
                schema: "app",
                table: "Predictions");

            migrationBuilder.RenameTable(
                name: "Predictions",
                schema: "app",
                newName: "ClassPrediction",
                newSchema: "app");

            migrationBuilder.RenameIndex(
                name: "IX_Predictions_DetectionId_Rank",
                schema: "app",
                table: "ClassPrediction",
                newName: "IX_ClassPrediction_DetectionId_Rank");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassPrediction",
                schema: "app",
                table: "ClassPrediction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassPrediction_Detections_DetectionId",
                schema: "app",
                table: "ClassPrediction",
                column: "DetectionId",
                principalSchema: "app",
                principalTable: "Detections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
