using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategyVersioningAndCRUD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "strategies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_favorite",
                table: "strategies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_backtested_at",
                table: "strategies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "strategies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parent_strategy_id",
                table: "strategies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "tags",
                table: "strategies",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version_number",
                table: "strategies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "strategy_comparisons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    strategy_ids = table.Column<int[]>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_comparisons", x => x.id);
                    table.ForeignKey(
                        name: "FK_strategy_comparisons_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategy_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_strategy_tags_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_strategies_is_archived",
                table: "strategies",
                column: "is_archived");

            migrationBuilder.CreateIndex(
                name: "ix_strategies_is_favorite",
                table: "strategies",
                column: "is_favorite");

            migrationBuilder.CreateIndex(
                name: "ix_strategies_last_backtested_at",
                table: "strategies",
                column: "last_backtested_at");

            migrationBuilder.CreateIndex(
                name: "ix_strategies_parent_strategy_id",
                table: "strategies",
                column: "parent_strategy_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_comparisons_created_at",
                table: "strategy_comparisons",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_comparisons_user_id",
                table: "strategy_comparisons",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_tags_user_id",
                table: "strategy_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_tags_user_id_name",
                table: "strategy_tags",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_strategies_strategies_parent_strategy_id",
                table: "strategies",
                column: "parent_strategy_id",
                principalTable: "strategies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_strategies_strategies_parent_strategy_id",
                table: "strategies");

            migrationBuilder.DropTable(
                name: "strategy_comparisons");

            migrationBuilder.DropTable(
                name: "strategy_tags");

            migrationBuilder.DropIndex(
                name: "ix_strategies_is_archived",
                table: "strategies");

            migrationBuilder.DropIndex(
                name: "ix_strategies_is_favorite",
                table: "strategies");

            migrationBuilder.DropIndex(
                name: "ix_strategies_last_backtested_at",
                table: "strategies");

            migrationBuilder.DropIndex(
                name: "ix_strategies_parent_strategy_id",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "is_favorite",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "last_backtested_at",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "parent_strategy_id",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "version_number",
                table: "strategies");
        }
    }
}
