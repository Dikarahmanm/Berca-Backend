using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Berca_Backend.Data;

/// <summary>
/// Database reset utility for development environment with enhanced seeding capabilities
/// </summary>
public static class DatabaseResetUtility
{
    /// <summary>
    /// Reset database and apply all migrations with fresh seeding
    /// Use this for clean development environment setup
    /// </summary>
    public static async Task ResetDatabaseAsync(AppDbContext context, ILogger logger, bool forceReset = false)
    {
        try
        {
            logger.LogInformation("🔄 Starting database reset operation...");

            if (!forceReset)
            {
                logger.LogWarning("⚠️ Database reset requires confirmation. Use forceReset=true to proceed");
                return;
            }

            // Step 1: Drop existing database if it exists
            logger.LogInformation("🗑️ Dropping existing database...");
            if (await context.Database.EnsureDeletedAsync())
            {
                logger.LogInformation("✅ Database dropped successfully");
            }
            else
            {
                logger.LogInformation("ℹ️ Database did not exist");
            }

            // Step 2: Apply all migrations
            logger.LogInformation("🔨 Applying all migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ All migrations applied successfully");

            // Step 3: Verify database structure
            logger.LogInformation("🔍 Verifying database structure...");
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database after migration");
            }

            // Step 4: Seed fresh data
            logger.LogInformation("🌱 Seeding fresh sample data...");
            await SampleDataSeeder.SeedSampleDataAsync(context, logger);
            logger.LogInformation("✅ Sample data seeded successfully");

            // Step 5: Verify data integrity
            await VerifyDataIntegrityAsync(context, logger);

            logger.LogInformation("🎉 Database reset completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Database reset failed");
            throw;
        }
    }

    /// <summary>
    /// Create fresh database with migrations and seeding if it doesn't exist
    /// Safe for production - won't drop existing database
    /// </summary>
    public static async Task EnsureDatabaseAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("🔍 Checking database existence...");

            // Check if database exists and is accessible
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogInformation("📦 Database doesn't exist, creating and seeding...");
                
                // Apply migrations
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Database created with migrations");

                // Seed initial data
                await SampleDataSeeder.SeedSampleDataAsync(context, logger);
                logger.LogInformation("✅ Initial data seeded");
            }
            else
            {
                logger.LogInformation("✅ Database exists and is accessible");
                
                // Apply any pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation($"🔄 Applying {pendingMigrations.Count()} pending migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("✅ Migrations applied successfully");
                }

                // Seed data if missing (idempotent)
                await SampleDataSeeder.SeedSampleDataAsync(context, logger);
            }

            await VerifyDataIntegrityAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Database setup failed");
            throw;
        }
    }

    /// <summary>
    /// Verify database integrity and data consistency
    /// </summary>
    public static async Task VerifyDataIntegrityAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("🔍 Verifying data integrity...");

            // Check critical tables exist and have data
            var checks = new Dictionary<string, Task<int>>
            {
                ["Categories"] = context.Categories.CountAsync(),
                ["Products"] = context.Products.CountAsync(),
                ["Branches"] = context.Branches.CountAsync(),
                ["Users"] = context.Users.CountAsync(),
                ["Members"] = context.Members.CountAsync(),
                ["Suppliers"] = context.Suppliers.CountAsync()
            };

            var results = new Dictionary<string, int>();
            foreach (var check in checks)
            {
                results[check.Key] = await check.Value;
            }

            // Log results
            logger.LogInformation("📊 Database integrity check results:");
            foreach (var result in results)
            {
                var status = result.Value > 0 ? "✅" : "⚠️";
                logger.LogInformation($"  {status} {result.Key}: {result.Value} records");
            }

            // Check for critical missing data
            var missingData = results.Where(r => r.Value == 0).Select(r => r.Key).ToList();
            if (missingData.Any())
            {
                logger.LogWarning($"⚠️ Missing data in tables: {string.Join(", ", missingData)}");
            }

            // Verify foreign key relationships
            await VerifyForeignKeyIntegrityAsync(context, logger);

            logger.LogInformation("✅ Data integrity verification completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Data integrity verification failed");
            throw;
        }
    }

    /// <summary>
    /// Verify foreign key relationships are valid
    /// </summary>
    private static async Task VerifyForeignKeyIntegrityAsync(AppDbContext context, ILogger logger)
    {
        var issues = new List<string>();

        // Check User-Branch relationships
        var usersWithInvalidBranches = await context.Users
            .Where(u => u.BranchId != null && !context.Branches.Any(b => b.Id == u.BranchId))
            .CountAsync();
        
        if (usersWithInvalidBranches > 0)
        {
            issues.Add($"Users with invalid BranchId: {usersWithInvalidBranches}");
        }

        // Check Product-Category relationships
        var productsWithInvalidCategories = await context.Products
            .Where(p => !context.Categories.Any(c => c.Id == p.CategoryId))
            .CountAsync();
        
        if (productsWithInvalidCategories > 0)
        {
            issues.Add($"Products with invalid CategoryId: {productsWithInvalidCategories}");
        }

        // Check Facture-Supplier relationships if they exist
        try
        {
            var facturesWithInvalidSuppliers = await context.Factures
                .Where(f => !context.Suppliers.Any(s => s.Id == f.SupplierId))
                .CountAsync();
            
            if (facturesWithInvalidSuppliers > 0)
            {
                issues.Add($"Factures with invalid SupplierId: {facturesWithInvalidSuppliers}");
            }
        }
        catch
        {
            // Factures table might not exist yet
        }

        if (issues.Any())
        {
            logger.LogWarning($"⚠️ Foreign key integrity issues found: {string.Join(", ", issues)}");
        }
        else
        {
            logger.LogInformation("✅ Foreign key integrity verified successfully");
        }
    }

    /// <summary>
    /// Get database status information
    /// </summary>
    public static async Task<DatabaseStatus> GetDatabaseStatusAsync(AppDbContext context)
    {
        var status = new DatabaseStatus();

        try
        {
            status.CanConnect = await context.Database.CanConnectAsync();
            
            if (status.CanConnect)
            {
                status.AppliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
                status.PendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
                
                // Get record counts
                status.RecordCounts = new Dictionary<string, int>
                {
                    ["Categories"] = await context.Categories.CountAsync(),
                    ["Products"] = await context.Products.CountAsync(),
                    ["Branches"] = await context.Branches.CountAsync(),
                    ["Users"] = await context.Users.CountAsync(),
                    ["Members"] = await context.Members.CountAsync(),
                    ["Suppliers"] = await context.Suppliers.CountAsync()
                };

                try
                {
                    status.RecordCounts["Factures"] = await context.Factures.CountAsync();
                }
                catch
                {
                    status.RecordCounts["Factures"] = -1; // Table doesn't exist
                }
            }
        }
        catch (Exception ex)
        {
            status.Error = ex.Message;
        }

        return status;
    }
}

/// <summary>
/// Database status information
/// </summary>
public class DatabaseStatus
{
    public bool CanConnect { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrations { get; set; } = new();
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public string? Error { get; set; }

    public bool HasPendingMigrations => PendingMigrations.Any();
    public bool IsHealthy => CanConnect && !HasPendingMigrations && string.IsNullOrEmpty(Error);
}