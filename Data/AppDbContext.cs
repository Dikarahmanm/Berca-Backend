// Data/AppDbContext.cs - Updated for Sprint 2
using Berca_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ==================== EXISTING DBSETS ==================== //
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<LogActivity> LogActivities { get; set; }
        public DbSet<Category> Categories { get; set; }

        // ==================== NEW SPRINT 2 DBSETS ==================== //
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MemberPoint> MemberPoints { get; set; }
        public DbSet<InventoryMutation> InventoryMutations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationSettings> NotificationSettings { get; set; }
        public DbSet<UserNotificationSettings> UserNotificationSettings { get; set; }
        public DbSet<Branch> Branches { get; set; }
        
        // ==================== INVENTORY TRANSFER DBSETS ==================== //
        public DbSet<InventoryTransfer> InventoryTransfers { get; set; }
        public DbSet<InventoryTransferItem> InventoryTransferItems { get; set; }
        public DbSet<InventoryTransferStatusHistory> InventoryTransferStatusHistories { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Branch configuration
            // Branch configuration
            // ===== BRANCH CONFIGURATION (NEW) ===== //
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.Property(b => b.BranchCode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(b => b.BranchCode)
                    .IsUnique()
                    .HasDatabaseName("IX_Branches_BranchCode");

                entity.Property(b => b.BranchName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(b => b.Address)
                    .IsRequired()
                    .HasMaxLength(300); // Increased for full addresses

                entity.Property(b => b.ManagerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(b => b.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(b => b.Email)
                    .HasMaxLength(100);

                // Location properties
                entity.Property(b => b.City)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(b => b.Province)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(b => b.PostalCode)
                    .IsRequired()
                    .HasMaxLength(10);

                // Store properties
                entity.Property(b => b.StoreSize)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Medium");

                entity.Property(b => b.EmployeeCount)
                    .HasDefaultValue(0);

                entity.Property(b => b.IsActive)
                    .HasDefaultValue(true);

                // Timezone-aware date properties (stored as UTC)
                entity.Property(b => b.OpeningDate)
                    .IsRequired()
                    .HasColumnType("datetime2");

                entity.Property(b => b.CreatedAt)
                    .IsRequired()
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(b => b.UpdatedAt)
                    .IsRequired()
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("GETUTCDATE()");

                // Self-referencing relationship (kept for flexibility but not used in flat structure)
                entity.HasOne(b => b.ParentBranch)
                    .WithMany(b => b.SubBranches)
                    .HasForeignKey(b => b.ParentBranchId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

                // Indexes for performance (retail chain queries)
                entity.HasIndex(b => b.City)
                    .HasDatabaseName("IX_Branches_City");

                entity.HasIndex(b => b.Province)
                    .HasDatabaseName("IX_Branches_Province");

                entity.HasIndex(b => b.BranchType)
                    .HasDatabaseName("IX_Branches_BranchType");

                entity.HasIndex(b => b.IsActive)
                    .HasDatabaseName("IX_Branches_IsActive");

                entity.HasIndex(b => new { b.Province, b.City })
                    .HasDatabaseName("IX_Branches_Province_City");
            });

            // ==================== EXISTING CONFIGURATIONS ==================== //

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("User");

                // NEW: Branch assignment properties
                entity.Property(u => u.CanAccessMultipleBranches)
                    .HasDefaultValue(false);

                entity.Property(u => u.AccessibleBranchIds)
                    .HasMaxLength(500); // JSON array storage

                entity.Property(u => u.IsActive)
                    .HasDefaultValue(true);

                // Branch relationship
                entity.HasOne(u => u.Branch)
                    .WithMany(b => b.Users)
                    .HasForeignKey(u => u.BranchId)
                    .OnDelete(DeleteBehavior.SetNull); // User stays when branch deleted

                // Indexes for user-branch queries
                entity.HasIndex(u => u.BranchId)
                    .HasDatabaseName("IX_Users_BranchId");

                entity.HasIndex(u => u.Role)
                    .HasDatabaseName("IX_Users_Role");

                entity.HasIndex(u => new { u.BranchId, u.Role })
                    .HasDatabaseName("IX_Users_BranchId_Role");
            });

            // UserProfile Configuration
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Department).HasMaxLength(50);
                entity.Property(e => e.Position).HasMaxLength(50);
                entity.Property(e => e.Division).HasMaxLength(50);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.PhotoUrl).HasMaxLength(255);

                entity.HasOne(p => p.User)
                      .WithOne(u => u.UserProfile)
                      .HasForeignKey<UserProfile>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Category Configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Categories_Name");
                entity.HasIndex(e => e.Color).HasDatabaseName("IX_Categories_Color");
            });

            // LogActivity Configuration
            modelBuilder.Entity<LogActivity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });

            // ==================== NEW SPRINT 2 CONFIGURATIONS ==================== //

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Barcode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Unit).HasMaxLength(20).HasDefaultValue("pcs");
                entity.Property(e => e.ImageUrl).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.Barcode).IsUnique().HasDatabaseName("IX_Products_Barcode");
                entity.HasIndex(e => e.Name).HasDatabaseName("IX_Products_Name");
                entity.HasIndex(e => e.CategoryId).HasDatabaseName("IX_Products_CategoryId");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Products_IsActive");

                // Relationships
                entity.HasOne(p => p.Category)
                      .WithMany() // Category doesn't have Products navigation property in Sprint 1
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Sale Configuration
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PaymentReference).HasMaxLength(100);
                entity.Property(e => e.CustomerName).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");

                // Indexes
                entity.HasIndex(e => e.SaleNumber).IsUnique().HasDatabaseName("IX_Sales_SaleNumber");
                entity.HasIndex(e => e.SaleDate).HasDatabaseName("IX_Sales_SaleDate");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Sales_Status");
                entity.HasIndex(e => e.PaymentMethod).HasDatabaseName("IX_Sales_PaymentMethod");

                // Relationships
                entity.HasOne(s => s.Cashier)
                      .WithMany()
                      .HasForeignKey(s => s.CashierId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Member)
                      .WithMany(m => m.Sales)
                      .HasForeignKey(s => s.MemberId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // SaleItem Configuration
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ProductBarcode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Unit).HasMaxLength(20).HasDefaultValue("pcs");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.SaleId).HasDatabaseName("IX_SaleItems_SaleId");
                entity.HasIndex(e => e.ProductId).HasDatabaseName("IX_SaleItems_ProductId");

                // Relationships
                entity.HasOne(si => si.Sale)
                      .WithMany(s => s.SaleItems)
                      .HasForeignKey(si => si.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(si => si.Product)
                      .WithMany(p => p.SaleItems)
                      .HasForeignKey(si => si.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Member Configuration
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.MemberNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.Phone).IsUnique().HasDatabaseName("IX_Members_Phone");
                entity.HasIndex(e => e.MemberNumber).IsUnique().HasDatabaseName("IX_Members_MemberNumber");
                entity.HasIndex(e => e.Email).HasDatabaseName("IX_Members_Email");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Members_IsActive");
            });

            // MemberPoint Configuration
            modelBuilder.Entity<MemberPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.MemberId).HasDatabaseName("IX_MemberPoints_MemberId");
                entity.HasIndex(e => e.Type).HasDatabaseName("IX_MemberPoints_Type");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_MemberPoints_CreatedAt");

                // Relationships
                entity.HasOne(mp => mp.Member)
                      .WithMany(m => m.MemberPoints)
                      .HasForeignKey(mp => mp.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mp => mp.Sale)
                      .WithMany()
                      .HasForeignKey(mp => mp.SaleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // InventoryMutation Configuration
            modelBuilder.Entity<InventoryMutation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Notes).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.ProductId).HasDatabaseName("IX_InventoryMutations_ProductId");
                entity.HasIndex(e => e.Type).HasDatabaseName("IX_InventoryMutations_Type");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_InventoryMutations_CreatedAt");

                // Relationships
                entity.HasOne(im => im.Product)
                      .WithMany(p => p.InventoryMutations)
                      .HasForeignKey(im => im.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(im => im.Sale)
                      .WithMany()
                      .HasForeignKey(im => im.SaleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Notification Configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.ActionUrl).HasMaxLength(500);
                entity.Property(e => e.ActionText).HasMaxLength(100);
                entity.Property(e => e.RelatedEntity).HasMaxLength(50);
                entity.Property(e => e.Metadata).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Notifications_UserId");
                entity.HasIndex(e => e.Type).HasDatabaseName("IX_Notifications_Type");
                entity.HasIndex(e => e.IsRead).HasDatabaseName("IX_Notifications_IsRead");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Notifications_CreatedAt");

                // Relationships
                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // NotificationSettings Configuration
            modelBuilder.Entity<NotificationSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PushToken).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("IX_NotificationSettings_UserId");

                // Relationships
                entity.HasOne(ns => ns.User)
                      .WithOne()
                      .HasForeignKey<NotificationSettings>(ns => ns.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== SEED DATA ==================== //

            // Existing Category Seed Data
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Makanan",
                    Color = "#FF914D",
                    Description = "Produk makanan dan snack",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 2,
                    Name = "Minuman",
                    Color = "#4BBF7B",
                    Description = "Minuman segar dan berenergi",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 3,
                    Name = "Elektronik",
                    Color = "#E15A4F",
                    Description = "Perangkat elektronik dan aksesoris",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 4,
                    Name = "Rumah Tangga",
                    Color = "#FFB84D",
                    Description = "Keperluan dan peralatan rumah tangga",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 5,
                    Name = "Kesehatan",
                    Color = "#6366F1",
                    Description = "Produk kesehatan dan perawatan",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            modelBuilder.Entity<UserNotificationSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(e => e.User)
                      .WithOne(u => u.UserNotificationSettings)   // add this nav on your User class
                      .HasForeignKey<UserNotificationSettings>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });




            // Sample Products Seed Data
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Indomie Ayam Bawang",
                    Barcode = "8886001001923",
                    Description = "Mie instan rasa ayam bawang",
                    BuyPrice = 2500,
                    SellPrice = 3500,
                    Stock = 50,
                    MinimumStock = 10,
                    Unit = "pcs",
                    CategoryId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 2,
                    Name = "Coca Cola 330ml",
                    Barcode = "8851013301234",
                    Description = "Minuman berkarbonasi rasa cola",
                    BuyPrice = 6000,
                    SellPrice = 8000,
                    Stock = 30,
                    MinimumStock = 5,
                    Unit = "pcs",
                    CategoryId = 2,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 3,
                    Name = "Baterai AA Alkaline",
                    Barcode = "1234567890123",
                    Description = "Baterai alkaline ukuran AA",
                    BuyPrice = 12000,
                    SellPrice = 15000,
                    Stock = 20,
                    MinimumStock = 5,
                    Unit = "pcs",
                    CategoryId = 3,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 4,
                    Name = "Sabun Cuci Piring Sunlight",
                    Barcode = "8992775123456",
                    Description = "Sabun pencuci piring konsentrat",
                    BuyPrice = 8500,
                    SellPrice = 12000,
                    Stock = 25,
                    MinimumStock = 5,
                    Unit = "pcs",
                    CategoryId = 4,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 5,
                    Name = "Paracetamol 500mg",
                    Barcode = "8992832123456",
                    Description = "Obat pereda nyeri dan demam",
                    BuyPrice = 3000,
                    SellPrice = 5000,
                    Stock = 40,
                    MinimumStock = 10,
                    Unit = "tablet",
                    CategoryId = 5,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}