using Microsoft.EntityFrameworkCore;
using Berca_Backend.Models;
using Microsoft.Extensions.Logging;

namespace Berca_Backend.Data;

public static class SampleDataSeeder
{
    /// <summary>
    /// Seeds sample data with proper identity column handling and comprehensive error handling
    /// </summary>
    public static async Task SeedSampleDataAsync(AppDbContext context, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("🌱 Starting sample data seeding process...");

            // Check if any critical data already exists
            if (await context.Users.AnyAsync())
            {
                logger?.LogInformation("✅ Sample data already exists, skipping seeding");
                return;
            }

            using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Seed Branches first (no dependencies)
                await SeedBranchesAsync(context, logger);
                await context.SaveChangesAsync();

                // 2. Seed Users (depends on branches)
                await SeedUsersAsync(context, logger);
                await context.SaveChangesAsync();

                // 3. Seed Members (no dependencies on users/branches)
                await SeedMembersAsync(context, logger);
                await context.SaveChangesAsync();

                // 4. Seed Suppliers (no dependencies)
                await SeedSuppliersAsync(context, logger);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
                logger?.LogInformation("✅ Sample data seeding completed successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger?.LogError(ex, "❌ Error during sample data seeding, transaction rolled back");
                throw;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ Failed to seed sample data");
            throw;
        }
    }

    /// <summary>
    /// Seeds branch data without explicit ID assignments
    /// </summary>
    private static async Task SeedBranchesAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Branches.AnyAsync())
        {
            logger?.LogInformation("⏭️ Branches already exist, skipping branch seeding");
            return;
        }

        logger?.LogInformation("🏢 Seeding branch data...");

        var branches = new List<Branch>
        {
            new Branch
            {
                BranchCode = "HQ",
                BranchName = "Head Office",
                BranchType = BranchType.Head,
                Address = "Jl. Raya Jakarta No. 123",
                ManagerName = "Budi Santoso",
                Phone = "021-1234567",
                Email = "hq@tokoeniwan.com",
                City = "Jakarta",
                Province = "DKI Jakarta",
                PostalCode = "12345",
                OpeningDate = DateTime.UtcNow.AddMonths(-12),
                StoreSize = "Large",
                EmployeeCount = 25,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UpdatedAt = DateTime.UtcNow.AddMonths(-12)
            },
            new Branch
            {
                BranchCode = "PWK-01",
                BranchName = "Toko Eniwan Purwakarta",
                BranchType = BranchType.Branch,
                Address = "Jl. Ahmad Yani No. 45, Purwakarta",
                ManagerName = "Siti Nurhaliza",
                Phone = "0264-123456",
                Email = "purwakarta@tokoeniwan.com",
                City = "Purwakarta",
                Province = "Jawa Barat",
                PostalCode = "41115",
                OpeningDate = DateTime.UtcNow.AddMonths(-8),
                StoreSize = "Medium",
                EmployeeCount = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                UpdatedAt = DateTime.UtcNow.AddMonths(-8)
            },
            new Branch
            {
                BranchCode = "BDG-01",
                BranchName = "Toko Eniwan Bandung",
                BranchType = BranchType.Branch,
                Address = "Jl. Braga No. 67, Bandung",
                ManagerName = "Ahmad Fauzi",
                Phone = "022-987654",
                Email = "bandung@tokoeniwan.com",
                City = "Bandung",
                Province = "Jawa Barat",
                PostalCode = "40111",
                OpeningDate = DateTime.UtcNow.AddMonths(-6),
                StoreSize = "Medium",
                EmployeeCount = 12,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new Branch
            {
                BranchCode = "SBY-01",
                BranchName = "Toko Eniwan Surabaya",
                BranchType = BranchType.Branch,
                Address = "Jl. Gubeng Pojok No. 88, Surabaya",
                ManagerName = "Rika Sari",
                Phone = "031-5678901",
                Email = "surabaya@tokoeniwan.com",
                City = "Surabaya",
                Province = "Jawa Timur",
                PostalCode = "60261",
                OpeningDate = DateTime.UtcNow.AddMonths(-4),
                StoreSize = "Large",
                EmployeeCount = 15,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddMonths(-4)
            }
        };

        context.Branches.AddRange(branches);
        logger?.LogInformation($"✅ Added {branches.Count} branches");
    }

    /// <summary>
    /// Seeds user data with proper branch relationships
    /// </summary>
    private static async Task SeedUsersAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Users.AnyAsync())
        {
            logger?.LogInformation("⏭️ Users already exist, skipping user seeding");
            return;
        }

        logger?.LogInformation("👤 Seeding user data...");

        // Get branches for assignment
        var headBranch = await context.Branches.FirstOrDefaultAsync(b => b.BranchType == BranchType.Head);
        var firstBranch = await context.Branches.FirstOrDefaultAsync(b => b.BranchType == BranchType.Branch);

        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                BranchId = null, // Admin can access all branches
                CanAccessMultipleBranches = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "cashier",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("cashier123"),
                Role = "User",
                BranchId = firstBranch?.Id, // Assign to first branch
                CanAccessMultipleBranches = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        logger?.LogInformation($"✅ Added {users.Count} users");
    }

    /// <summary>
    /// Seeds member data without identity column issues
    /// </summary>
    private static async Task SeedMembersAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Members.AnyAsync())
        {
            logger?.LogInformation("⏭️ Members already exist, skipping member seeding");
            return;
        }

        logger?.LogInformation("👥 Seeding member data...");

        var members = new List<Member>
        {
            new Member
            {
                Name = "John Doe",
                Phone = "08123456789",
                Email = "john.doe@email.com",
                Address = "Jl. Sudirman No. 123, Jakarta",
                DateOfBirth = new DateTime(1985, 5, 15),
                Gender = "Male",
                MemberNumber = "MBR001",
                Tier = MembershipTier.Bronze,
                JoinDate = DateTime.UtcNow.AddMonths(-6),
                IsActive = true,
                TotalPoints = 150,
                UsedPoints = 50,
                TotalSpent = 1500000,
                TotalTransactions = 25,
                LastTransactionDate = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Member
            {
                Name = "Jane Smith",
                Phone = "08567890123",
                Email = "jane.smith@email.com",
                Address = "Jl. Thamrin No. 456, Jakarta",
                DateOfBirth = new DateTime(1990, 8, 22),
                Gender = "Female",
                MemberNumber = "MBR002",
                Tier = MembershipTier.Silver,
                JoinDate = DateTime.UtcNow.AddMonths(-10),
                IsActive = true,
                TotalPoints = 750,
                UsedPoints = 250,
                TotalSpent = 5500000,
                TotalTransactions = 87,
                LastTransactionDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Member
            {
                Name = "Bob Wilson",
                Phone = "08912345678",
                Email = "bob.wilson@email.com",
                Address = "Jl. Gatot Subroto No. 789, Jakarta",
                DateOfBirth = new DateTime(1978, 12, 3),
                Gender = "Male",
                MemberNumber = "MBR003",
                Tier = MembershipTier.Gold,
                JoinDate = DateTime.UtcNow.AddYears(-2),
                IsActive = true,
                TotalPoints = 2500,
                UsedPoints = 1000,
                TotalSpent = 15000000,
                TotalTransactions = 234,
                LastTransactionDate = DateTime.UtcNow.AddHours(-5),
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-5),
                CreatedBy = "System",
                UpdatedBy = "System"
            }
        };

        context.Members.AddRange(members);
        logger?.LogInformation($"✅ Added {members.Count} members");
    }

    /// <summary>
    /// Seeds supplier data with proper foreign key references to Users
    /// </summary>
    private static async Task SeedSuppliersAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Suppliers.AnyAsync())
        {
            logger?.LogInformation("⏭️ Suppliers already exist, skipping supplier seeding");
            return;
        }

        logger?.LogInformation("🏪 Seeding supplier data...");

        // Get actual User IDs from the database after user seeding
        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "admin");
        
        var cashierUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "cashier");

        if (adminUser == null)
        {
            logger?.LogError("❌ Admin user not found. Cannot create suppliers without valid CreatedBy reference.");
            throw new InvalidOperationException("Admin user must exist before creating suppliers");
        }

        if (cashierUser == null)
        {
            logger?.LogError("❌ Cashier user not found. Using admin user as fallback for all suppliers.");
            cashierUser = adminUser; // Fallback to admin user
        }

        logger?.LogInformation($"📋 Using User IDs - Admin: {adminUser.Id}, Cashier: {cashierUser.Id}");

        var suppliers = new List<Supplier>
        {
            new Supplier
            {
                SupplierCode = "SUP001",
                CompanyName = "PT Distributor Makanan Nusantara",
                ContactPerson = "Ahmad Wijaya",
                Phone = "021-5551234",
                Email = "ahmad@distributormakanan.com",
                Address = "Jl. Industri No. 15, Jakarta Timur",
                PaymentTerms = 30,
                CreditLimit = 50000000,
                CreatedBy = adminUser.Id, // Use actual admin user ID
                IsActive = true
            },
            new Supplier
            {
                SupplierCode = "SUP002",
                CompanyName = "CV Elektronik Sejahtera",
                ContactPerson = "Sari Dewi",
                Phone = "022-4567890",
                Email = "sari@elektroniksejahtera.com",
                Address = "Jl. Elektronik No. 88, Bandung",
                PaymentTerms = 45,
                CreditLimit = 75000000,
                CreatedBy = cashierUser.Id, // Use actual cashier user ID
                IsActive = true
            },
            new Supplier
            {
                SupplierCode = "SUP003",
                CompanyName = "PT Rumah Tangga Prima",
                ContactPerson = "Budi Hartono",
                Phone = "031-3334444",
                Email = "budi@rumahtanggaprima.com",
                Address = "Jl. Industri Rumah Tangga No. 45, Surabaya",
                PaymentTerms = 60,
                CreditLimit = 25000000,
                CreatedBy = adminUser.Id, // Use actual admin user ID
                IsActive = true
            }
        };

        context.Suppliers.AddRange(suppliers);
        logger?.LogInformation($"✅ Added {suppliers.Count} suppliers with proper User references");
    }

    /// <summary>
    /// Fix existing users with NULL branch assignments
    /// </summary>
    public static async Task FixNullBranchAssignmentsAsync(AppDbContext context, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("🔧 Checking for users with NULL branch assignments...");

            var usersWithoutBranch = await context.Users
                .Where(u => u.BranchId == null && !u.CanAccessMultipleBranches)
                .ToListAsync();

            if (!usersWithoutBranch.Any())
            {
                logger?.LogInformation("✅ No users with NULL branch assignments found");
                return;
            }

            var defaultBranch = await context.Branches
                .FirstOrDefaultAsync(b => b.BranchType == BranchType.Branch);

            if (defaultBranch == null)
            {
                logger?.LogWarning("⚠️ No branch found to assign users to");
                return;
            }

            foreach (var user in usersWithoutBranch)
            {
                user.BranchId = defaultBranch.Id;
                user.UpdatedAt = DateTime.UtcNow;
                logger?.LogInformation($"🔄 Assigned user '{user.Username}' to branch '{defaultBranch.BranchName}'");
            }

            await context.SaveChangesAsync();
            logger?.LogInformation($"✅ Fixed {usersWithoutBranch.Count} users with NULL branch assignments");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ Failed to fix NULL branch assignments");
            throw;
        }
    }
}