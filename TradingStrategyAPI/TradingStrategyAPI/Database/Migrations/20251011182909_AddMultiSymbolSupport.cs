using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSymbolSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_es_futures_bars",
                table: "es_futures_bars");

            migrationBuilder.DropIndex(
                name: "ix_bars_timestamp_volume",
                table: "es_futures_bars");

            migrationBuilder.RenameTable(
                name: "es_futures_bars",
                newName: "futures_bars");

            migrationBuilder.AddColumn<string>(
                name: "symbol",
                table: "futures_bars",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "ES");  // Set existing bars to ES symbol by default

            migrationBuilder.AddPrimaryKey(
                name: "pk_futures_bars",
                table: "futures_bars",
                columns: new[] { "symbol", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_bars_symbol",
                table: "futures_bars",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_bars_symbol_timestamp",
                table: "futures_bars",
                columns: new[] { "symbol", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_bars_symbol_timestamp_volume",
                table: "futures_bars",
                columns: new[] { "symbol", "timestamp", "volume" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_futures_bars",
                table: "futures_bars");

            migrationBuilder.DropIndex(
                name: "ix_bars_symbol",
                table: "futures_bars");

            migrationBuilder.DropIndex(
                name: "ix_bars_symbol_timestamp",
                table: "futures_bars");

            migrationBuilder.DropIndex(
                name: "ix_bars_symbol_timestamp_volume",
                table: "futures_bars");

            migrationBuilder.DropColumn(
                name: "symbol",
                table: "futures_bars");

            migrationBuilder.RenameTable(
                name: "futures_bars",
                newName: "es_futures_bars");

            migrationBuilder.AddPrimaryKey(
                name: "PK_es_futures_bars",
                table: "es_futures_bars",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_bars_timestamp_volume",
                table: "es_futures_bars",
                columns: new[] { "timestamp", "volume" });
        }
    }
}
