using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeAnalysisAndEnhancedTradeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "chart_data_end",
                table: "trade_results",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "chart_data_start",
                table: "trade_results",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entry_bar_index",
                table: "trade_results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entry_quality_score",
                table: "trade_results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "exit_bar_index",
                table: "trade_results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "exit_quality_score",
                table: "trade_results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "indicator_values",
                table: "trade_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "risk_reward_ratio",
                table: "trade_results",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "setup_bars",
                table: "trade_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trade_bars",
                table: "trade_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trade_notes",
                table: "trade_results",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "trade_analyses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trade_result_id = table.Column<int>(type: "integer", nullable: false),
                    entry_reason = table.Column<string>(type: "text", nullable: false),
                    exit_reason = table.Column<string>(type: "text", nullable: false),
                    market_condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    time_of_day = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    day_of_week = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    vix_level = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    adx_value = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    atr_value = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    what_went_wrong = table.Column<string>(type: "text", nullable: true),
                    what_went_right = table.Column<string>(type: "text", nullable: true),
                    narrative = table.Column<string>(type: "text", nullable: true),
                    lessons_learned = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_trade_analyses_trade_results_trade_result_id",
                        column: x => x.trade_result_id,
                        principalTable: "trade_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_trade_analyses_condition_time",
                table: "trade_analyses",
                columns: new[] { "market_condition", "time_of_day" });

            migrationBuilder.CreateIndex(
                name: "ix_trade_analyses_day_of_week",
                table: "trade_analyses",
                column: "day_of_week");

            migrationBuilder.CreateIndex(
                name: "ix_trade_analyses_market_condition",
                table: "trade_analyses",
                column: "market_condition");

            migrationBuilder.CreateIndex(
                name: "ix_trade_analyses_time_of_day",
                table: "trade_analyses",
                column: "time_of_day");

            migrationBuilder.CreateIndex(
                name: "ix_trade_analyses_trade_result_id",
                table: "trade_analyses",
                column: "trade_result_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trade_analyses");

            migrationBuilder.DropColumn(
                name: "chart_data_end",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "chart_data_start",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "entry_bar_index",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "entry_quality_score",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "exit_bar_index",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "exit_quality_score",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "indicator_values",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "risk_reward_ratio",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "setup_bars",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "trade_bars",
                table: "trade_results");

            migrationBuilder.DropColumn(
                name: "trade_notes",
                table: "trade_results");
        }
    }
}
