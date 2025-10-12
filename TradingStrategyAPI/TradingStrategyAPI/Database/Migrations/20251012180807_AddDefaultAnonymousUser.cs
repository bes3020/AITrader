using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingStrategyAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultAnonymousUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert default anonymous user with ID 1
            // Password hash is a dummy value since this user cannot authenticate
            migrationBuilder.Sql(@"
                INSERT INTO users (id, email, password_hash, created_at)
                VALUES (1, 'anonymous@tradingstrategy.local', 'N/A', NOW())
                ON CONFLICT (id) DO NOTHING;

                -- Reset the sequence to start from 2 for real users
                SELECT setval(pg_get_serial_sequence('users', 'id'), GREATEST(2, (SELECT MAX(id) FROM users) + 1), false);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the anonymous user
            migrationBuilder.Sql(@"
                DELETE FROM users WHERE id = 1 AND email = 'anonymous@tradingstrategy.local';
            ");
        }
    }
}
