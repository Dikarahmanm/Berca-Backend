using System;
using Microsoft.EntityFrameworkCore.Migrations;

// This manual migration creates missing tables used by SmartNotificationEngine:
// - NotificationRules
// - UserNotificationPreferences

namespace Berca_Backend.Migrations
{
    public partial class AddSmartNotificationTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create NotificationRules if it doesn't exist
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NotificationRules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NotificationRules](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RuleType] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL,
        [Parameters] NVARCHAR(2000) NULL,
        [TargetRoles] NVARCHAR(200) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END
            ");

            // Create UserNotificationPreferences if it doesn't exist
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserNotificationPreferences]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserNotificationPreferences](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [EmailNotifications] BIT NOT NULL,
        [PushNotifications] BIT NOT NULL,
        [SmsNotifications] BIT NOT NULL,
        [ExpiryAlerts] BIT NOT NULL,
        [StockAlerts] BIT NOT NULL,
        [FinancialAlerts] BIT NOT NULL,
        [AlertFrequency] NVARCHAR(20) NOT NULL,
        [QuietHoursStart] NVARCHAR(5) NOT NULL,
        [QuietHoursEnd] NVARCHAR(5) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [FK_UserNotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_UserNotificationPreferences_UserId] ON [dbo].[UserNotificationPreferences]([UserId]);
END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[UserNotificationPreferences]', N'U') IS NOT NULL
    DROP TABLE [dbo].[UserNotificationPreferences];
IF OBJECT_ID(N'[dbo].[NotificationRules]', N'U') IS NOT NULL
    DROP TABLE [dbo].[NotificationRules];
            ");
        }
    }
}

