using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class CleanupCalendarEventModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing UpdatedBy column if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[CalendarEvents]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [CalendarEvents] ADD [UpdatedBy] int NULL
                END
            ");

            // Add foreign key constraint for UpdatedBy
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CalendarEvents_Users_UpdatedBy')
                BEGIN
                    ALTER TABLE [CalendarEvents] 
                    ADD CONSTRAINT [FK_CalendarEvents_Users_UpdatedBy] 
                    FOREIGN KEY ([UpdatedBy]) REFERENCES [Users]([Id]) ON DELETE SET NULL
                END
            ");

            // Create indexes for performance
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CalendarEvents_StartDate')
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_CalendarEvents_StartDate] ON [CalendarEvents] ([StartDate])
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CalendarEvents_EventType')
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_CalendarEvents_EventType] ON [CalendarEvents] ([EventType])
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CalendarEvents_RelatedEntity')
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_CalendarEvents_RelatedEntity] ON [CalendarEvents] ([RelatedEntityType], [RelatedEntityId]) WHERE [RelatedEntityType] IS NOT NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
