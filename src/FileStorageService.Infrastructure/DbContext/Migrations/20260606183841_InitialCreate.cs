using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileStorageService.Infrastructure.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_ContentType",
                table: "StoredFiles",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_CreatedAtUtc",
                table: "StoredFiles",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_CreatedByUserId",
                table: "StoredFiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_DeletedAtUtc",
                table: "StoredFiles",
                column: "DeletedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredFiles");
        }
    }
}
