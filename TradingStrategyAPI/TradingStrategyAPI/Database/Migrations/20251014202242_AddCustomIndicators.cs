using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomIndicators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "custom_indicator_id",
                table: "conditions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "custom_indicators",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: false),
                    formula = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_indicators", x => x.id);
                    table.ForeignKey(
                        name: "FK_custom_indicators_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conditions_custom_indicator_id",
                table: "conditions",
                column: "custom_indicator_id");

            migrationBuilder.CreateIndex(
                name: "ix_custom_indicators_created_at",
                table: "custom_indicators",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_custom_indicators_is_public",
                table: "custom_indicators",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "ix_custom_indicators_type",
                table: "custom_indicators",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_custom_indicators_user_id",
                table: "custom_indicators",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_custom_indicators_user_id_name",
                table: "custom_indicators",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_conditions_custom_indicators_custom_indicator_id",
                table: "conditions",
                column: "custom_indicator_id",
                principalTable: "custom_indicators",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conditions_custom_indicators_custom_indicator_id",
                table: "conditions");

            migrationBuilder.DropTable(
                name: "custom_indicators");

            migrationBuilder.DropIndex(
                name: "ix_conditions_custom_indicator_id",
                table: "conditions");

            migrationBuilder.DropColumn(
                name: "custom_indicator_id",
                table: "conditions");
        }
    }
}
