using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixCalendarEventForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the incorrect foreign key constraint
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CalendarEvents_Users_CreatedByUserId')
                BEGIN
                    ALTER TABLE [CalendarEvents] DROP CONSTRAINT [FK_CalendarEvents_Users_CreatedByUserId]
                END
            ");

            // Drop the incorrect column CreatedByUserId if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[CalendarEvents]') AND name = 'CreatedByUserId')
                BEGIN
                    ALTER TABLE [CalendarEvents] DROP COLUMN [CreatedByUserId]
                END
            ");

            // Note: FK_CalendarEvents_Users_CreatedBy constraint was added manually during emergency fix
            // This migration step is skipped as the constraint already exists and is working
            migrationBuilder.Sql(@"
                -- Verify the constraint exists (no-op if already exists)
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CalendarEvents_Users_CreatedBy')
                BEGIN
                    -- Constraint exists - migration step completed manually
                    PRINT 'FK_CalendarEvents_Users_CreatedBy already exists - OK'
                END
                ELSE
                BEGIN
                    -- Add the constraint if it somehow doesn't exist
                    ALTER TABLE [CalendarEvents] 
                    ADD CONSTRAINT [FK_CalendarEvents_Users_CreatedBy] 
                    FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert changes - drop the correct foreign key
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CalendarEvents_Users_CreatedBy')
                BEGIN
                    ALTER TABLE [CalendarEvents] DROP CONSTRAINT [FK_CalendarEvents_Users_CreatedBy]
                END
            ");

            // Re-add the CreatedByUserId column if needed (for rollback)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[CalendarEvents]') AND name = 'CreatedByUserId')
                BEGIN
                    ALTER TABLE [CalendarEvents] ADD [CreatedByUserId] int NULL
                END
            ");

            // Re-add the incorrect foreign key (for complete rollback)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CalendarEvents_Users_CreatedByUserId')
                BEGIN
                    ALTER TABLE [CalendarEvents] 
                    ADD CONSTRAINT [FK_CalendarEvents_Users_CreatedByUserId] 
                    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
                END
            ");
        }
    }
}
