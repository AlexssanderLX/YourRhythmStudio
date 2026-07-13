using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class EnsureLandingTracksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `admin_settings` (
                    `Key` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
                    `Value` varchar(500) CHARACTER SET utf8mb4 NULL,
                    `UpdatedAtUtc` datetime(6) NOT NULL,
                    CONSTRAINT `PK_admin_settings` PRIMARY KEY (`Key`)
                ) CHARACTER SET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `landing_tracks` (
                    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
                    `Title` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
                    `FileName` varchar(180) CHARACTER SET utf8mb4 NOT NULL,
                    `SortOrder` int NOT NULL,
                    `UploadedAtUtc` datetime(6) NOT NULL,
                    CONSTRAINT `PK_landing_tracks` PRIMARY KEY (`Id`)
                ) CHARACTER SET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                SET @landing_tracks_sort_order_index_exists = (
                    SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'landing_tracks'
                      AND INDEX_NAME = 'IX_landing_tracks_SortOrder'
                );
                """);

            migrationBuilder.Sql(
                """
                SET @landing_tracks_sort_order_index_sql = IF(
                    @landing_tracks_sort_order_index_exists = 0,
                    'CREATE INDEX `IX_landing_tracks_SortOrder` ON `landing_tracks` (`SortOrder`)',
                    'SELECT 1'
                );
                """);

            migrationBuilder.Sql("PREPARE landing_tracks_sort_order_index_stmt FROM @landing_tracks_sort_order_index_sql;");
            migrationBuilder.Sql("EXECUTE landing_tracks_sort_order_index_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE landing_tracks_sort_order_index_stmt;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "landing_tracks");
            migrationBuilder.DropTable(name: "admin_settings");
        }
    }
}
