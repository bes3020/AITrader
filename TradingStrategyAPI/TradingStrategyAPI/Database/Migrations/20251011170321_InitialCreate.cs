using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "es_futures_bars",
                columns: table => new
                {
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    open = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    high = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    low = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    close = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    volume = table.Column<long>(type: "bigint", nullable: false),
                    vwap = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ema9 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ema20 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ema50 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    avg_volume_20 = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_es_futures_bars", x => x.timestamp);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "strategies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_positions = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    position_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategies", x => x.id);
                    table.ForeignKey(
                        name: "FK_strategies_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conditions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    indicator = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(20)", maxLength: 20, nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    strategy_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conditions", x => x.id);
                    table.ForeignKey(
                        name: "FK_conditions_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalTable: "strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stop_losses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    strategy_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stop_losses", x => x.id);
                    table.ForeignKey(
                        name: "FK_stop_losses_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalTable: "strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategy_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    strategy_id = table.Column<int>(type: "integer", nullable: false),
                    total_trades = table.Column<int>(type: "integer", nullable: false),
                    win_rate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    total_pnl = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    avg_win = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    avg_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    max_drawdown = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    profit_factor = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    sharpe_ratio = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    insights = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    backtest_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    backtest_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_strategy_results_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalTable: "strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "take_profits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    strategy_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_take_profits", x => x.id);
                    table.ForeignKey(
                        name: "FK_take_profits_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalTable: "strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    exit_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    entry_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    exit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    pnl = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bars_held = table.Column<int>(type: "integer", nullable: false),
                    max_adverse_excursion = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    max_favorable_excursion = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    strategy_result_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_trade_results_strategy_results_strategy_result_id",
                        column: x => x.strategy_result_id,
                        principalTable: "strategy_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conditions_indicator",
                table: "conditions",
                column: "indicator");

            migrationBuilder.CreateIndex(
                name: "ix_conditions_strategy_id",
                table: "conditions",
                column: "strategy_id");

            migrationBuilder.CreateIndex(
                name: "ix_bars_timestamp",
                table: "es_futures_bars",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_bars_timestamp_volume",
                table: "es_futures_bars",
                columns: new[] { "timestamp", "volume" });

            migrationBuilder.CreateIndex(
                name: "ix_stop_losses_strategy_id",
                table: "stop_losses",
                column: "strategy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_strategies_created_at",
                table: "strategies",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_strategies_is_active",
                table: "strategies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_strategies_user_id",
                table: "strategies",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_results_created_at",
                table: "strategy_results",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_results_strategy_id",
                table: "strategy_results",
                column: "strategy_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_results_strategy_id_created_at",
                table: "strategy_results",
                columns: new[] { "strategy_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_take_profits_strategy_id",
                table: "take_profits",
                column: "strategy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trade_results_entry_time",
                table: "trade_results",
                column: "entry_time");

            migrationBuilder.CreateIndex(
                name: "ix_trade_results_result",
                table: "trade_results",
                column: "result");

            migrationBuilder.CreateIndex(
                name: "ix_trade_results_strategy_result_id",
                table: "trade_results",
                column: "strategy_result_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_results_strategy_result_id_result_pnl",
                table: "trade_results",
                columns: new[] { "strategy_result_id", "result", "pnl" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conditions");

            migrationBuilder.DropTable(
                name: "es_futures_bars");

            migrationBuilder.DropTable(
                name: "stop_losses");

            migrationBuilder.DropTable(
                name: "take_profits");

            migrationBuilder.DropTable(
                name: "trade_results");

            migrationBuilder.DropTable(
                name: "strategy_results");

            migrationBuilder.DropTable(
                name: "strategies");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
