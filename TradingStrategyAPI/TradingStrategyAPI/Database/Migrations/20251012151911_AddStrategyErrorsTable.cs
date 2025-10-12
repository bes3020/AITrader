using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategyErrorsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StrategyErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StrategyId = table.Column<int>(type: "integer", nullable: true),
                    ErrorType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    FailedExpression = table.Column<string>(type: "text", nullable: true),
                    SuggestedFix = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Context = table.Column<string>(type: "text", nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyErrors_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_strategy_errors_error_type",
                table: "StrategyErrors",
                column: "ErrorType");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_errors_error_type_timestamp",
                table: "StrategyErrors",
                columns: new[] { "ErrorType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_strategy_errors_is_resolved",
                table: "StrategyErrors",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_errors_strategy_id",
                table: "StrategyErrors",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_errors_timestamp",
                table: "StrategyErrors",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrategyErrors");
        }
    }
}
