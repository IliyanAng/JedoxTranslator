using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JedoxTranslator.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourceTexts",
                columns: table => new
                {
                    SID = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceTexts", x => x.SID);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SID = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LangId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Translations_SourceTexts_SID",
                        column: x => x.SID,
                        principalTable: "SourceTexts",
                        principalColumn: "SID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SourceTexts",
                columns: new[] { "SID", "Text" },
                values: new object[,]
                {
                    { "goodbye_message", "Goodbye" },
                    { "welcome_message", "Welcome to Jedox Translator" }
                });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "LangId", "SID", "TranslatedText" },
                values: new object[,]
                {
                    { 1, "de-DE", "welcome_message", "Willkommen bei Jedox Translator" },
                    { 2, "de-DE", "goodbye_message", "Auf Wiedersehen" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Translations_SID_LangId",
                table: "Translations",
                columns: new[] { "SID", "LangId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.DropTable(
                name: "SourceTexts");
        }
    }
}
