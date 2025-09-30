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
        
        // ADD missing StockMutations DbSet that is referenced in ProductController
        public DbSet<StockMutation> StockMutations { get; set; }
        
        // ==================== INVENTORY TRANSFER DBSETS ==================== //
        public DbSet<InventoryTransfer> InventoryTransfers { get; set; }
        public DbSet<InventoryTransferItem> InventoryTransferItems { get; set; }
        public DbSet<InventoryTransferStatusHistory> InventoryTransferStatusHistories { get; set; }
        
        // ==================== EXPIRY MANAGEMENT DBSETS ==================== //
        public DbSet<ProductBatch> ProductBatches { get; set; }
        
        // ==================== BATCH TRACKING DBSETS ==================== //
        public DbSet<SaleItemBatch> SaleItemBatches { get; set; }
        
        // ==================== SUPPLIER MANAGEMENT DBSETS ==================== //
        public DbSet<Supplier> Suppliers { get; set; }
        
        // ==================== FACTURE MANAGEMENT DBSETS ==================== //
        public DbSet<Facture> Factures { get; set; }
        public DbSet<FactureItem> FactureItems { get; set; }
        public DbSet<FacturePayment> FacturePayments { get; set; }
        
        // ✅ ADD missing PurchaseOrders DbSet that is referenced in SupplierController
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

        // ==================== MEMBER CREDIT SYSTEM DBSETS ==================== //
        public DbSet<MemberCreditTransaction> MemberCreditTransactions { get; set; }
        public DbSet<MemberPaymentReminder> MemberPaymentReminders { get; set; }
        
        // ==================== PUSH NOTIFICATION SYSTEM DBSETS ==================== //
        public DbSet<PushSubscription> PushSubscriptions { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<PushNotificationLog> PushNotificationLogs { get; set; }
        
        // ==================== CALENDAR & EVENTS SYSTEM DBSETS ==================== //
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<CalendarEventReminder> CalendarEventReminders { get; set; }
        
        // ==================== REPORTING SYSTEM DBSETS ==================== //
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportExecution> ReportExecutions { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        
        // ==================== SMART NOTIFICATION SYSTEM DBSETS ==================== //
        public DbSet<NotificationRule> NotificationRules { get; set; }
        public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; }
        
        // ==================== MULTI-BRANCH INTEGRATION DBSETS ==================== //
        public DbSet<BranchAccess> BranchAccesses { get; set; }
        public DbSet<TransferRequest> TransferRequests { get; set; }
        public DbSet<TransferItem> TransferItems { get; set; }

        // ==================== BRANCH-SPECIFIC INVENTORY DBSETS ==================== //
        public DbSet<BranchInventory> BranchInventories { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Get environment for configuration
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = environment == "Development";
            
            // Note: Query splitting behavior will be configured in Program.cs
            // optionsBuilder.UseQuerySplittingBehavior is SQL Server specific
            
            // Enhanced logging for development
            if (isDevelopment)
            {
                optionsBuilder
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
            
            // Configure warnings
            optionsBuilder.ConfigureWarnings(warnings =>
            {
                // Handle pending model changes warning
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                
                // Handle multiple collection include warning based on environment
                if (isDevelopment)
                {
                    // Log in development for awareness
                    warnings.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
                }
                else
                {
                    // Suppress in production to reduce log noise
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
                }
            });
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure database indexes for optimal performance
            // DatabaseIndexes.ConfigureIndexes(modelBuilder); // Temporarily commented until model properties are updated
            // Branch configuration
            // Branch configuration
            // ===== BRANCH CONFIGURATION (NEW) ===== //
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Id).ValueGeneratedOnAdd(); // Explicit identity column configuration

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
                entity.Property(u => u.Id).ValueGeneratedOnAdd(); // Explicit identity column configuration

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

            // ==================== COMPREHENSIVE INDONESIAN MINIMARKET CATEGORIES ==================== //
            modelBuilder.Entity<Category>().HasData(
                // ===== FOOD & SNACKS CATEGORIES (RequiresExpiryDate = true, DefaultWarning = 30 days) =====
                new Category
                {
                    Id = 1,
                    Name = "Makanan Instan",
                    Color = "#FF6B35",
                    Description = "Mie instan, nasi instan, bubur instan - Indomie, Pop Mie, Sedaap, Sarimi",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 2,
                    Name = "Makanan Kaleng",
                    Color = "#FF8E53",
                    Description = "Kornet, sarden, buah kaleng, sayur kaleng - Pronas, ABC, Ayam Brand",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 3,
                    Name = "Snacks & Keripik",
                    Color = "#FFA726",
                    Description = "Chitato, Taro, Qtela, Lay's, keripik tradisional, kacang-kacangan",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 4,
                    Name = "Biskuit & Wafer",
                    Color = "#FFB74D",
                    Description = "Roma, Monde, Khong Guan, Oreo, wafer Tanggo, Marie Regal",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 5,
                    Name = "Permen & Coklat",
                    Color = "#8D4E85",
                    Description = "Kopiko, Ricola, Cadbury, SilverQueen, permen lokal, Mentos",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 6,
                    Name = "Kue & Roti",
                    Color = "#D2691E",
                    Description = "Kue kering, roti tawar, roti manis, donat, cake - Sari Roti, Breadtalk",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 7,
                    Name = "Makanan Beku",
                    Color = "#4FC3F7",
                    Description = "Nugget, sosis, bakso beku, frozen food - Fiesta, Bernardi, So Good",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // ===== BEVERAGE CATEGORIES (RequiresExpiryDate = true, DefaultWarning = 60 days) =====
                new Category
                {
                    Id = 8,
                    Name = "Air Mineral",
                    Color = "#29B6F6",
                    Description = "Aqua, VIT, Club, Pristine, Le Minerale, Cleo",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 9,
                    Name = "Minuman Ringan",
                    Color = "#E53935",
                    Description = "Coca Cola, Sprite, Fanta, 7UP, Pepsi, Mirinda",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 10,
                    Name = "Teh & Kopi Kemasan",
                    Color = "#6D4C41",
                    Description = "Teh Botol, Ultra Teh, Good Day, Kapal Api, Nescafe, Teh Pucuk",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 11,
                    Name = "Susu & Dairy",
                    Color = "#FFF8E1",
                    Description = "Ultra Milk, Indomilk, Frisian Flag, susu kental manis - Carnation, Cap Enak",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 12,
                    Name = "Minuman Isotonik",
                    Color = "#00BCD4",
                    Description = "Pocari Sweat, Mizone, Hydro Coco, Ion Water, Revive",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 13,
                    Name = "Jus & Minuman Buah",
                    Color = "#FF7043",
                    Description = "Buavita, SunTop, Minute Maid, Okky Jelly Drink, Frestea",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 14,
                    Name = "Minuman Energi",
                    Color = "#D32F2F",
                    Description = "Kratingdaeng, M-150, Extra Joss, Red Bull, Shark",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 15,
                    Name = "Es Krim",
                    Color = "#E1BEE7",
                    Description = "Walls, Aice, Diamond, Campina, Magnum, Cornetto",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 60,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // ===== HEALTH & MEDICINE CATEGORIES (RequiresExpiryDate = true, DefaultWarning = 90 days) =====
                new Category
                {
                    Id = 16,
                    Name = "Obat Bebas",
                    Color = "#4CAF50",
                    Description = "Paracetamol, Panadol, Bodrex, Paramex, Aspirin, Ibuprofen",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 90,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 17,
                    Name = "Obat Flu & Batuk",
                    Color = "#66BB6A",
                    Description = "Mixagrip, Neozep, Woods, Vicks, Komix, Actifed",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 90,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 18,
                    Name = "Obat Pencernaan",
                    Color = "#81C784",
                    Description = "Promag, Mylanta, Antasida, Norit, Entrostop, Diapet",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 90,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 19,
                    Name = "Vitamin & Suplemen",
                    Color = "#A5D6A7",
                    Description = "Redoxon, CDR, Enervon-C, Sangobion, Blackmores, Imboost",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 90,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 20,
                    Name = "Perawatan Luka",
                    Color = "#C8E6C9",
                    Description = "Plester, perban, betadine, alkohol, kapas, hansaplast",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 90,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 21,
                    Name = "Hand Sanitizer & Antiseptik",
                    Color = "#E8F5E8",
                    Description = "Dettol, Antis, Lifebuoy, Nuvo, Mama Lime, Biore",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 90,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // ===== PERSONAL CARE CATEGORIES (RequiresExpiryDate = true, DefaultWarning = 180 days) =====
                new Category
                {
                    Id = 22,
                    Name = "Shampo & Hair Care",
                    Color = "#2196F3",
                    Description = "Pantene, Head & Shoulders, Sunsilk, Clear, Tresemme, Makarizo",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 23,
                    Name = "Sabun Mandi",
                    Color = "#42A5F5",
                    Description = "Lux, Dove, Lifebuoy, Giv, Dettol, Biore, Citra",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 24,
                    Name = "Pasta Gigi",
                    Color = "#64B5F6",
                    Description = "Pepsodent, Close Up, Formula, Sensodyne, Systema, Enzim",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 25,
                    Name = "Body Lotion & Skin Care",
                    Color = "#90CAF9",
                    Description = "Vaseline, Nivea, Citra, Pond's, Olay, Garnier",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 26,
                    Name = "Deodorant",
                    Color = "#BBDEFB",
                    Description = "Rexona, Dove Men, Gillette, Axe, Nivea Men, Adidas",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 27,
                    Name = "Kosmetik & Makeup",
                    Color = "#E91E63",
                    Description = "Wardah, Pixy, Maybelline, Revlon, L'Oreal, Make Over",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 28,
                    Name = "Parfum & Cologne",
                    Color = "#F06292",
                    Description = "Axe, Rexona, Body Shop, Calvin Klein, Hugo Boss, local brands",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 180,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // ===== BABY & KIDS CATEGORIES (RequiresExpiryDate = true, DefaultWarning = 45 days) =====
                new Category
                {
                    Id = 29,
                    Name = "Susu Formula",
                    Color = "#FFE0B2",
                    Description = "SGM, Dancow, Bebelac, Lactogen, Nutrilon, Enfamil",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 45,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 30,
                    Name = "Makanan Bayi",
                    Color = "#FFCC80",
                    Description = "Cerelac, Milna, Promina, SUN, Heinz, Gerber",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 45,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 31,
                    Name = "Popok & Diapers",
                    Color = "#FFB74D",
                    Description = "Pampers, MamyPoko, Sweety, Merries, Goon, Huggies",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 45,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 32,
                    Name = "Baby Care Products",
                    Color = "#FFA726",
                    Description = "Baby oil, powder, lotion, shampoo - Johnson's, Cussons, Zwitsal",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 45,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // ===== HOUSEHOLD & CLEANING CATEGORIES (RequiresExpiryDate = true, DefaultWarning = 365 days) =====
                new Category
                {
                    Id = 33,
                    Name = "Deterjen & Sabun Cuci",
                    Color = "#9C27B0",
                    Description = "Rinso, Attack, Surf, So Klin, Daia, Total",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 365,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 34,
                    Name = "Pembersih Piring",
                    Color = "#AB47BC",
                    Description = "Sunlight, Mama Lemon, Cream, Joy, Soklin, Economic",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 365,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 35,
                    Name = "Pembersih Lantai",
                    Color = "#BA68C8",
                    Description = "Vixal, Super Pel, Wipol, Karbol, Kispray, Stella",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 365,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 36,
                    Name = "Pembersih Kamar Mandi",
                    Color = "#CE93D8",
                    Description = "Vixal, Harpic, Domestos, Duck, Toilet Duck, Cif",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 365,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 37,
                    Name = "Pelembut & Pewangi Pakaian",
                    Color = "#E1BEE7",
                    Description = "Molto, Downy, Soklin, Comfort, Rapika, Stella",
                    RequiresExpiryDate = true,
                    DefaultExpiryWarningDays = 365,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // ===== NON-EXPIRY CATEGORIES (RequiresExpiryDate = false) =====
                new Category
                {
                    Id = 38,
                    Name = "Elektronik & Gadget",
                    Color = "#607D8B",
                    Description = "Powerbank, charger, earphone, speaker, flashdisk, mouse",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 39,
                    Name = "Aksesoris HP",
                    Color = "#78909C",
                    Description = "Case, screen protector, holder, cable, tempered glass, ring holder",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 40,
                    Name = "Rokok & Tembakau",
                    Color = "#8D6E63",
                    Description = "Gudang Garam, Djarum, Marlboro, Sampoerna, Bentoel, Lucky Strike",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 41,
                    Name = "Alat Tulis",
                    Color = "#FF9800",
                    Description = "Pensil, pulpen, buku, penggaris, penghapus, spidol - Faber Castell, Pilot",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 42,
                    Name = "Perlengkapan Rumah",
                    Color = "#795548",
                    Description = "Tissue, toilet paper, kantong plastik, aluminum foil, plastic wrap",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 43,
                    Name = "Seasonal & Gift Items",
                    Color = "#F44336",
                    Description = "Kartu ucapan, gift wrap, balon, hiasan, mainan kecil, souvenir",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 44,
                    Name = "Baterai & Lampu",
                    Color = "#FFEB3B",
                    Description = "Baterai ABC, Energizer, Panasonic, lampu LED, senter, bohlam",
                    RequiresExpiryDate = false,
                    DefaultExpiryWarningDays = 30,
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

            // ==================== PRODUCT BATCH CONFIGURATION ==================== //
            modelBuilder.Entity<ProductBatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Product relationship
                entity.HasOne(pb => pb.Product)
                      .WithMany(p => p.ProductBatches)
                      .HasForeignKey(pb => pb.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Branch relationship
                entity.HasOne(pb => pb.Branch)
                      .WithMany()
                      .HasForeignKey(pb => pb.BranchId)
                      .OnDelete(DeleteBehavior.SetNull);

                // User relationships
                entity.HasOne(pb => pb.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(pb => pb.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pb => pb.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(pb => pb.UpdatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pb => pb.DisposedByUser)
                      .WithMany()
                      .HasForeignKey(pb => pb.DisposedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Property configurations
                entity.Property(pb => pb.BatchNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(pb => pb.CostPerUnit)
                      .HasColumnType("decimal(18,4)");

                entity.Property(pb => pb.SupplierName)
                      .HasMaxLength(100);

                entity.Property(pb => pb.PurchaseOrderNumber)
                      .HasMaxLength(50);

                entity.Property(pb => pb.Notes)
                      .HasMaxLength(500);

                entity.Property(pb => pb.BlockReason)
                      .HasMaxLength(200);

                entity.Property(pb => pb.DisposalMethod)
                      .HasMaxLength(100);

                entity.Property(pb => pb.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(pb => pb.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for performance
                entity.HasIndex(pb => pb.ProductId)
                      .HasDatabaseName("IX_ProductBatches_ProductId");

                entity.HasIndex(pb => pb.BatchNumber)
                      .HasDatabaseName("IX_ProductBatches_BatchNumber");

                entity.HasIndex(pb => pb.ExpiryDate)
                      .HasDatabaseName("IX_ProductBatches_ExpiryDate");

                entity.HasIndex(pb => pb.BranchId)
                      .HasDatabaseName("IX_ProductBatches_BranchId");

                entity.HasIndex(pb => pb.IsExpired)
                      .HasDatabaseName("IX_ProductBatches_IsExpired");

                entity.HasIndex(pb => pb.IsDisposed)
                      .HasDatabaseName("IX_ProductBatches_IsDisposed");

                entity.HasIndex(pb => new { pb.ProductId, pb.ExpiryDate })
                      .HasDatabaseName("IX_ProductBatches_ProductId_ExpiryDate");

                entity.HasIndex(pb => new { pb.BranchId, pb.ExpiryDate })
                      .HasDatabaseName("IX_ProductBatches_BranchId_ExpiryDate");
            });

            // ==================== INVENTORY TRANSFER CONFIGURATION ==================== //
            modelBuilder.Entity<InventoryTransfer>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Column mappings for different names between model and database
                entity.Property(it => it.SourceBranchId).HasColumnName("FromBranchId");
                entity.Property(it => it.DestinationBranchId).HasColumnName("ToBranchId");
                
                // Map user ID properties to correct integer columns
                entity.Property(it => it.RequestedBy).HasColumnName("RequestedByUserId");
                entity.Property(it => it.ApprovedBy).HasColumnName("ApprovedByUserId");
                entity.Property(it => it.ShippedBy).HasColumnName("ShippedByUserId");
                entity.Property(it => it.ReceivedBy).HasColumnName("ReceivedByUserId");
                entity.Property(it => it.CancelledBy).HasColumnName("CancelledByUserId");
                
                // Relationships
                entity.HasOne(it => it.SourceBranch)
                      .WithMany()
                      .HasForeignKey(it => it.SourceBranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(it => it.DestinationBranch)
                      .WithMany()
                      .HasForeignKey(it => it.DestinationBranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(it => it.RequestedByUser)
                      .WithMany()
                      .HasForeignKey(it => it.RequestedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(it => it.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(it => it.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(it => it.ShippedByUser)
                      .WithMany()
                      .HasForeignKey(it => it.ShippedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(it => it.ReceivedByUser)
                      .WithMany()
                      .HasForeignKey(it => it.ReceivedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(it => it.CancelledByUser)
                      .WithMany()
                      .HasForeignKey(it => it.CancelledBy)
                      .OnDelete(DeleteBehavior.NoAction);
                      
                // Property configurations
                entity.Property(e => e.TransferNumber)
                      .IsRequired()
                      .HasMaxLength(50);
                      
                entity.Property(e => e.RequestReason)
                      .IsRequired()
                      .HasMaxLength(500);
                      
                entity.Property(e => e.Notes)
                      .HasMaxLength(1000);
                      
                entity.Property(e => e.DistanceKm)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);
                      
                entity.Property(e => e.EstimatedCost)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);
                      
                entity.Property(e => e.ActualCost)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                // String properties
                entity.Property(e => e.CancellationReason)
                      .HasMaxLength(500);

                entity.Property(e => e.LogisticsProvider)
                      .HasMaxLength(100);

                entity.Property(e => e.TrackingNumber)
                      .HasMaxLength(100);

                // Indexes for performance
                entity.HasIndex(e => e.TransferNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_InventoryTransfers_TransferNumber");

                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_InventoryTransfers_Status");

                entity.HasIndex(e => e.SourceBranchId)
                      .HasDatabaseName("IX_InventoryTransfers_SourceBranchId");

                entity.HasIndex(e => e.DestinationBranchId)
                      .HasDatabaseName("IX_InventoryTransfers_DestinationBranchId");

                entity.HasIndex(e => e.CreatedAt)
                      .HasDatabaseName("IX_InventoryTransfers_CreatedAt");
            });

            modelBuilder.Entity<InventoryTransferItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Relationships
                entity.HasOne(iti => iti.InventoryTransfer)
                      .WithMany(it => it.TransferItems)
                      .HasForeignKey(iti => iti.InventoryTransferId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(iti => iti.Product)
                      .WithMany()
                      .HasForeignKey(iti => iti.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Property configurations
                entity.Property(e => e.Quantity)
                      .HasColumnType("decimal(18,4)")
                      .HasDefaultValue(0);
                
                entity.Property(e => e.UnitCost)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.TotalCost)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                // Indexes
                entity.HasIndex(e => e.InventoryTransferId)
                      .HasDatabaseName("IX_InventoryTransferItems_InventoryTransferId");

                entity.HasIndex(e => e.ProductId)
                      .HasDatabaseName("IX_InventoryTransferItems_ProductId");
            });

            modelBuilder.Entity<InventoryTransferStatusHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Relationships
                entity.HasOne(itsh => itsh.InventoryTransfer)
                      .WithMany()
                      .HasForeignKey(itsh => itsh.InventoryTransferId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(itsh => itsh.ChangedByUser)
                      .WithMany()
                      .HasForeignKey(itsh => itsh.ChangedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                // Property configurations
                entity.Property(e => e.Reason)
                      .HasMaxLength(500);

                // Indexes
                entity.HasIndex(e => e.InventoryTransferId)
                      .HasDatabaseName("IX_InventoryTransferStatusHistories_InventoryTransferId");

                entity.HasIndex(e => e.ChangedBy)
                      .HasDatabaseName("IX_InventoryTransferStatusHistories_ChangedBy");

                entity.HasIndex(e => e.ChangedAt)
                      .HasDatabaseName("IX_InventoryTransferStatusHistories_ChangedAt");
            });

            // ==================== REALISTIC INDONESIAN MINIMARKET PRODUCTS ==================== //
            modelBuilder.Entity<Product>().HasData(
                // Food Products (Categories 1-7)
                new Product
                {
                    Id = 1,
                    Name = "Indomie Ayam Bawang",
                    Barcode = "8886001001923",
                    Description = "Mie instan rasa ayam bawang - Indofood",
                    BuyPrice = 2500,
                    SellPrice = 3500,
                    Stock = 50,
                    MinimumStock = 10,
                    Unit = "pcs",
                    CategoryId = 1, // Makanan Instan
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 2,
                    Name = "Sarimi Ayam Bawang",
                    Barcode = "8888001234567",
                    Description = "Mie instan kuah rasa ayam bawang - Sarimi",
                    BuyPrice = 2300,
                    SellPrice = 3200,
                    Stock = 40,
                    MinimumStock = 10,
                    Unit = "pcs",
                    CategoryId = 1, // Makanan Instan
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 3,
                    Name = "Pronas Kornet Sapi",
                    Barcode = "8992843287654",
                    Description = "Kornet sapi kaleng 198g - Pronas",
                    BuyPrice = 18000,
                    SellPrice = 25000,
                    Stock = 24,
                    MinimumStock = 5,
                    Unit = "kaleng",
                    CategoryId = 2, // Makanan Kaleng
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 4,
                    Name = "Chitato Rasa BBQ",
                    Barcode = "8999999876543",
                    Description = "Keripik kentang rasa BBQ - Chitato",
                    BuyPrice = 8500,
                    SellPrice = 12000,
                    Stock = 30,
                    MinimumStock = 8,
                    Unit = "pcs",
                    CategoryId = 3, // Snacks & Keripik
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 5,
                    Name = "Roma Kelapa",
                    Barcode = "8992753147258",
                    Description = "Biskuit kelapa - Roma Mayora",
                    BuyPrice = 4500,
                    SellPrice = 6500,
                    Stock = 36,
                    MinimumStock = 12,
                    Unit = "bks",
                    CategoryId = 4, // Biskuit & Wafer
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // Beverage Products (Categories 8-15)
                new Product
                {
                    Id = 6,
                    Name = "Aqua 600ml",
                    Barcode = "8992787134567",
                    Description = "Air mineral kemasan botol 600ml - Aqua",
                    BuyPrice = 2500,
                    SellPrice = 3500,
                    Stock = 48,
                    MinimumStock = 12,
                    Unit = "btl",
                    CategoryId = 8, // Air Mineral
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 7,
                    Name = "Coca Cola 330ml",
                    Barcode = "8851013301234",
                    Description = "Minuman berkarbonasi rasa cola - Coca Cola",
                    BuyPrice = 6000,
                    SellPrice = 8500,
                    Stock = 30,
                    MinimumStock = 6,
                    Unit = "btl",
                    CategoryId = 9, // Minuman Ringan
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 8,
                    Name = "Teh Botol Sosro 450ml",
                    Barcode = "8991002101234",
                    Description = "Teh kemasan botol rasa manis - Sosro",
                    BuyPrice = 4500,
                    SellPrice = 6500,
                    Stock = 24,
                    MinimumStock = 6,
                    Unit = "btl",
                    CategoryId = 10, // Teh & Kopi Kemasan
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 9,
                    Name = "Ultra Milk Coklat 250ml",
                    Barcode = "8992761456789",
                    Description = "Susu UHT rasa coklat - Ultra Milk",
                    BuyPrice = 5500,
                    SellPrice = 7500,
                    Stock = 30,
                    MinimumStock = 8,
                    Unit = "kotak",
                    CategoryId = 11, // Susu & Dairy
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 10,
                    Name = "Pocari Sweat 350ml",
                    Barcode = "8992696789012",
                    Description = "Minuman isotonik elektrolit - Pocari Sweat",
                    BuyPrice = 7000,
                    SellPrice = 10000,
                    Stock = 20,
                    MinimumStock = 5,
                    Unit = "btl",
                    CategoryId = 12, // Minuman Isotonik
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // Health Products (Categories 16-21)
                new Product
                {
                    Id = 11,
                    Name = "Panadol Tablet",
                    Barcode = "8992832123456",
                    Description = "Obat pereda nyeri dan demam - Panadol",
                    BuyPrice = 12000,
                    SellPrice = 16000,
                    Stock = 25,
                    MinimumStock = 5,
                    Unit = "strip",
                    CategoryId = 16, // Obat Bebas
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 12,
                    Name = "Mixagrip Flu & Batuk",
                    Barcode = "8992747369852",
                    Description = "Obat flu dan batuk - Mixagrip",
                    BuyPrice = 8500,
                    SellPrice = 12000,
                    Stock = 20,
                    MinimumStock = 5,
                    Unit = "strip",
                    CategoryId = 17, // Obat Flu & Batuk
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 13,
                    Name = "Redoxon Vitamin C",
                    Barcode = "8992888147258",
                    Description = "Vitamin C 1000mg - Redoxon",
                    BuyPrice = 45000,
                    SellPrice = 65000,
                    Stock = 15,
                    MinimumStock = 3,
                    Unit = "btl",
                    CategoryId = 19, // Vitamin & Suplemen
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // Personal Care Products (Categories 22-28)
                new Product
                {
                    Id = 14,
                    Name = "Pantene Shampo 170ml",
                    Barcode = "8992777456789",
                    Description = "Shampo rambut total damage care - Pantene",
                    BuyPrice = 18000,
                    SellPrice = 25000,
                    Stock = 18,
                    MinimumStock = 4,
                    Unit = "btl",
                    CategoryId = 22, // Shampo & Hair Care
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 15,
                    Name = "Lux Sabun Mandi",
                    Barcode = "8992556789012",
                    Description = "Sabun mandi soft touch - Lux",
                    BuyPrice = 4500,
                    SellPrice = 6500,
                    Stock = 30,
                    MinimumStock = 8,
                    Unit = "pcs",
                    CategoryId = 23, // Sabun Mandi
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 16,
                    Name = "Pepsodent 190g",
                    Barcode = "8992334567890",
                    Description = "Pasta gigi pencegah gigi berlubang - Pepsodent",
                    BuyPrice = 12000,
                    SellPrice = 16000,
                    Stock = 20,
                    MinimumStock = 5,
                    Unit = "tube",
                    CategoryId = 24, // Pasta Gigi
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // Household Products (Categories 33-37)
                new Product
                {
                    Id = 17,
                    Name = "Rinso Anti Noda 800g",
                    Barcode = "8992775987654",
                    Description = "Deterjen bubuk anti noda - Rinso",
                    BuyPrice = 15000,
                    SellPrice = 21000,
                    Stock = 25,
                    MinimumStock = 5,
                    Unit = "bks",
                    CategoryId = 33, // Deterjen & Sabun Cuci
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 18,
                    Name = "Sunlight 755ml",
                    Barcode = "8992775123456",
                    Description = "Sabun pencuci piring konsentrat - Sunlight",
                    BuyPrice = 8500,
                    SellPrice = 12000,
                    Stock = 22,
                    MinimumStock = 5,
                    Unit = "btl",
                    CategoryId = 34, // Pembersih Piring
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // Non-Expiry Products (Categories 38-44)
                new Product
                {
                    Id = 19,
                    Name = "Powerbank Xiaomi 10000mAh",
                    Barcode = "6941059648208",
                    Description = "Powerbank portabel 10000mAh - Xiaomi",
                    BuyPrice = 180000,
                    SellPrice = 250000,
                    Stock = 8,
                    MinimumStock = 2,
                    Unit = "pcs",
                    CategoryId = 38, // Elektronik & Gadget
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 20,
                    Name = "Gudang Garam Surya 16",
                    Barcode = "8992704987654",
                    Description = "Rokok kretek filter - Gudang Garam",
                    BuyPrice = 18000,
                    SellPrice = 20000,
                    Stock = 50,
                    MinimumStock = 10,
                    Unit = "bks",
                    CategoryId = 40, // Rokok & Tembakau
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 21,
                    Name = "Baterai ABC AA",
                    Barcode = "8992804321098",
                    Description = "Baterai alkaline ukuran AA - ABC",
                    BuyPrice = 8000,
                    SellPrice = 12000,
                    Stock = 40,
                    MinimumStock = 10,
                    Unit = "pack",
                    CategoryId = 44, // Baterai & Lampu
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 22,
                    Name = "Pulpen Standard AE7",
                    Barcode = "8999812345678",
                    Description = "Pulpen standard warna biru - Standard",
                    BuyPrice = 1500,
                    SellPrice = 2500,
                    Stock = 50,
                    MinimumStock = 15,
                    Unit = "pcs",
                    CategoryId = 41, // Alat Tulis
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // ==================== SUPPLIER CONFIGURATION ==================== //
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Property configurations
                entity.Property(e => e.SupplierCode)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.CompanyName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.ContactPerson)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Phone)
                      .IsRequired()
                      .HasMaxLength(15);

                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Address)
                      .HasMaxLength(500);

                entity.Property(e => e.CreditLimit)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for performance
                entity.HasIndex(e => e.SupplierCode)
                      .IsUnique()
                      .HasDatabaseName("IX_Suppliers_SupplierCode");

                entity.HasIndex(e => e.CompanyName)
                      .HasDatabaseName("IX_Suppliers_CompanyName");

                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Suppliers_Email");

                entity.HasIndex(e => new { e.BranchId, e.IsActive })
                      .HasDatabaseName("IX_Suppliers_Branch_Status");

                entity.HasIndex(e => e.PaymentTerms)
                      .HasDatabaseName("IX_Suppliers_PaymentTerms");

                entity.HasIndex(e => e.CreditLimit)
                      .HasDatabaseName("IX_Suppliers_CreditLimit");

                // Relationships
                entity.HasOne(s => s.Branch)
                      .WithMany()
                      .HasForeignKey(s => s.BranchId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(s => s.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(s => s.UpdatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== FACTURE MANAGEMENT CONFIGURATION ==================== //
            
            // Facture Configuration
            modelBuilder.Entity<Facture>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Property configurations
                entity.Property(e => e.SupplierInvoiceNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.InternalReferenceNumber)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.SupplierPONumber)
                      .HasMaxLength(50);

                entity.Property(e => e.DeliveryNoteNumber)
                      .HasMaxLength(50);

                entity.Property(e => e.TotalAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.PaidAmount)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0);

                entity.Property(e => e.Tax)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0);

                entity.Property(e => e.Discount)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0);

                entity.Property(e => e.Description)
                      .HasMaxLength(2000);

                entity.Property(e => e.Notes)
                      .HasMaxLength(2000);

                entity.Property(e => e.DisputeReason)
                      .HasMaxLength(1000);

                entity.Property(e => e.SupplierInvoiceFile)
                      .HasMaxLength(500);

                entity.Property(e => e.ReceiptFile)
                      .HasMaxLength(500);

                entity.Property(e => e.SupportingDocs)
                      .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasOne(f => f.Supplier)
                      .WithMany(s => s.Factures)
                      .HasForeignKey(f => f.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Branch)
                      .WithMany()
                      .HasForeignKey(f => f.BranchId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(f => f.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(f => f.UpdatedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(f => f.ReceivedByUser)
                      .WithMany()
                      .HasForeignKey(f => f.ReceivedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(f => f.VerifiedByUser)
                      .WithMany()
                      .HasForeignKey(f => f.VerifiedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(f => f.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(f => f.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                // Indexes for performance
                entity.HasIndex(f => f.InternalReferenceNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_Factures_InternalReferenceNumber");

                entity.HasIndex(f => new { f.SupplierId, f.SupplierInvoiceNumber })
                      .IsUnique()
                      .HasDatabaseName("IX_Factures_Supplier_InvoiceNumber");

                entity.HasIndex(f => f.Status)
                      .HasDatabaseName("IX_Factures_Status");

                entity.HasIndex(f => f.DueDate)
                      .HasDatabaseName("IX_Factures_DueDate");

                entity.HasIndex(f => f.InvoiceDate)
                      .HasDatabaseName("IX_Factures_InvoiceDate");

                entity.HasIndex(f => f.BranchId)
                      .HasDatabaseName("IX_Factures_BranchId");

                entity.HasIndex(f => f.TotalAmount)
                      .HasDatabaseName("IX_Factures_TotalAmount");

                entity.HasIndex(f => new { f.Status, f.DueDate })
                      .HasDatabaseName("IX_Factures_Status_DueDate");

                entity.HasIndex(f => new { f.BranchId, f.Status })
                      .HasDatabaseName("IX_Factures_Branch_Status");
            });

            // FactureItem Configuration
            modelBuilder.Entity<FactureItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Property configurations
                entity.Property(e => e.SupplierItemCode)
                      .HasMaxLength(100);

                entity.Property(e => e.SupplierItemDescription)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(e => e.Quantity)
                      .HasColumnType("decimal(18,4)");

                entity.Property(e => e.UnitPrice)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ReceivedQuantity)
                      .HasColumnType("decimal(18,4)");

                entity.Property(e => e.AcceptedQuantity)
                      .HasColumnType("decimal(18,4)");

                entity.Property(e => e.TaxRate)
                      .HasColumnType("decimal(5,2)")
                      .HasDefaultValue(0);

                entity.Property(e => e.DiscountAmount)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0);

                entity.Property(e => e.Notes)
                      .HasMaxLength(1000);

                entity.Property(e => e.VerificationNotes)
                      .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasOne(fi => fi.Facture)
                      .WithMany(f => f.Items)
                      .HasForeignKey(fi => fi.FactureId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fi => fi.Product)
                      .WithMany()
                      .HasForeignKey(fi => fi.ProductId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(fi => fi.VerifiedByUser)
                      .WithMany()
                      .HasForeignKey(fi => fi.VerifiedBy)
                      .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(fi => fi.FactureId)
                      .HasDatabaseName("IX_FactureItems_FactureId");

                entity.HasIndex(fi => fi.ProductId)
                      .HasDatabaseName("IX_FactureItems_ProductId");

                entity.HasIndex(fi => fi.IsVerified)
                      .HasDatabaseName("IX_FactureItems_IsVerified");
            });

            // FacturePayment Configuration
            modelBuilder.Entity<FacturePayment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Property configurations
                entity.Property(e => e.Amount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.OurPaymentReference)
                      .HasMaxLength(100);

                entity.Property(e => e.SupplierAckReference)
                      .HasMaxLength(100);

                entity.Property(e => e.BankAccount)
                      .HasMaxLength(100);

                entity.Property(e => e.CheckNumber)
                      .HasMaxLength(50);

                entity.Property(e => e.TransferReference)
                      .HasMaxLength(100);

                entity.Property(e => e.Notes)
                      .HasMaxLength(1000);

                entity.Property(e => e.FailureReason)
                      .HasMaxLength(1000);

                entity.Property(e => e.DisputeReason)
                      .HasMaxLength(500);

                entity.Property(e => e.PaymentReceiptFile)
                      .HasMaxLength(500);

                entity.Property(e => e.ConfirmationFile)
                      .HasMaxLength(500);

                entity.Property(e => e.RecurrencePattern)
                      .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasOne(fp => fp.Facture)
                      .WithMany(f => f.Payments)
                      .HasForeignKey(fp => fp.FactureId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fp => fp.ProcessedByUser)
                      .WithMany()
                      .HasForeignKey(fp => fp.ProcessedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fp => fp.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(fp => fp.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(fp => fp.ConfirmedByUser)
                      .WithMany()
                      .HasForeignKey(fp => fp.ConfirmedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(fp => fp.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(fp => fp.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fp => fp.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(fp => fp.UpdatedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(fp => fp.FactureId)
                      .HasDatabaseName("IX_FacturePayments_FactureId");

                entity.HasIndex(fp => fp.PaymentDate)
                      .HasDatabaseName("IX_FacturePayments_PaymentDate");

                entity.HasIndex(fp => fp.Status)
                      .HasDatabaseName("IX_FacturePayments_Status");

                entity.HasIndex(fp => fp.PaymentMethod)
                      .HasDatabaseName("IX_FacturePayments_PaymentMethod");

                entity.HasIndex(fp => new { fp.Status, fp.PaymentDate })
                      .HasDatabaseName("IX_FacturePayments_Status_PaymentDate");
            });

            // ==================== MEMBER CREDIT TRANSACTION CONFIGURATION ==================== //
            modelBuilder.Entity<MemberCreditTransaction>(entity =>
            {
                entity.HasKey(mct => mct.Id);
                entity.Property(mct => mct.Id).ValueGeneratedOnAdd();

                entity.Property(mct => mct.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(mct => mct.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(mct => mct.ReferenceNumber)
                    .HasMaxLength(100);

                entity.Property(mct => mct.Notes)
                    .HasMaxLength(1000);

                // Relationships
                entity.HasOne(mct => mct.Member)
                    .WithMany(m => m.CreditTransactions)
                    .HasForeignKey(mct => mct.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mct => mct.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(mct => mct.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mct => mct.Branch)
                    .WithMany()
                    .HasForeignKey(mct => mct.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for performance
                entity.HasIndex(mct => mct.MemberId)
                    .HasDatabaseName("IX_MemberCreditTransactions_MemberId");

                entity.HasIndex(mct => mct.TransactionDate)
                    .HasDatabaseName("IX_MemberCreditTransactions_TransactionDate");

                entity.HasIndex(mct => mct.Type)
                    .HasDatabaseName("IX_MemberCreditTransactions_Type");

                entity.HasIndex(mct => mct.Status)
                    .HasDatabaseName("IX_MemberCreditTransactions_Status");

                entity.HasIndex(mct => mct.DueDate)
                    .HasDatabaseName("IX_MemberCreditTransactions_DueDate");

                entity.HasIndex(mct => new { mct.MemberId, mct.Type, mct.Status })
                    .HasDatabaseName("IX_MemberCreditTransactions_Member_Type_Status");

                entity.HasIndex(mct => new { mct.Type, mct.DueDate })
                    .HasDatabaseName("IX_MemberCreditTransactions_Type_DueDate");
            });

            // ==================== MEMBER PAYMENT REMINDER CONFIGURATION ==================== //
            modelBuilder.Entity<MemberPaymentReminder>(entity =>
            {
                entity.HasKey(mpr => mpr.Id);
                entity.Property(mpr => mpr.Id).ValueGeneratedOnAdd();

                entity.Property(mpr => mpr.DueAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(mpr => mpr.ResponseAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(mpr => mpr.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(mpr => mpr.ContactMethod)
                    .HasMaxLength(100);

                entity.Property(mpr => mpr.Notes)
                    .HasMaxLength(500);

                // Relationships
                entity.HasOne(mpr => mpr.Member)
                    .WithMany(m => m.PaymentReminders)
                    .HasForeignKey(mpr => mpr.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mpr => mpr.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(mpr => mpr.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mpr => mpr.Branch)
                    .WithMany()
                    .HasForeignKey(mpr => mpr.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for collections and reminder management
                entity.HasIndex(mpr => mpr.MemberId)
                    .HasDatabaseName("IX_MemberPaymentReminders_MemberId");

                entity.HasIndex(mpr => mpr.ReminderDate)
                    .HasDatabaseName("IX_MemberPaymentReminders_ReminderDate");

                entity.HasIndex(mpr => mpr.NextReminderDate)
                    .HasDatabaseName("IX_MemberPaymentReminders_NextReminderDate");

                entity.HasIndex(mpr => mpr.Status)
                    .HasDatabaseName("IX_MemberPaymentReminders_Status");

                entity.HasIndex(mpr => mpr.ReminderType)
                    .HasDatabaseName("IX_MemberPaymentReminders_ReminderType");

                entity.HasIndex(mpr => mpr.Priority)
                    .HasDatabaseName("IX_MemberPaymentReminders_Priority");

                entity.HasIndex(mpr => new { mpr.MemberId, mpr.Status })
                    .HasDatabaseName("IX_MemberPaymentReminders_Member_Status");

                entity.HasIndex(mpr => new { mpr.NextReminderDate, mpr.Status })
                    .HasDatabaseName("IX_MemberPaymentReminders_NextReminder_Status");
            });

            // ==================== QUERY OPTIMIZATION CONFIGURATION ==================== //
            ConfigureQueryOptimization(modelBuilder);
        }

        /// <summary>
        /// Configure query behavior for optimal performance with multiple collection includes
        /// </summary>
        private void ConfigureQueryOptimization(ModelBuilder modelBuilder)
        {
            // Member entity with payment reminders optimization
            modelBuilder.Entity<Member>()
                .Navigation(m => m.PaymentReminders)
                .EnableLazyLoading(false); // Disable lazy loading for better control

            modelBuilder.Entity<Member>()
                .Navigation(m => m.CreditTransactions)
                .EnableLazyLoading(false);

            modelBuilder.Entity<Member>()
                .Navigation(m => m.MemberPoints)
                .EnableLazyLoading(false);

            modelBuilder.Entity<Member>()
                .Navigation(m => m.Sales)
                .EnableLazyLoading(false);

            // Sale entity optimizations
            modelBuilder.Entity<Sale>()
                .Navigation(s => s.SaleItems)
                .EnableLazyLoading(false);

            // Product entity optimizations for inventory queries
            modelBuilder.Entity<Product>()
                .Navigation(p => p.ProductBatches)
                .EnableLazyLoading(false);

            // Inventory Transfer optimizations
            modelBuilder.Entity<InventoryTransfer>()
                .Navigation(it => it.TransferItems)
                .EnableLazyLoading(false);

            // Note: StatusHistories navigation may not exist in current model

            // Report entity optimizations
            modelBuilder.Entity<Report>()
                .Navigation(r => r.ReportExecutions)
                .EnableLazyLoading(false);

            // ==================== CALENDAR EVENT CONFIGURATION ==================== //
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Property configurations
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ActionUrl).HasMaxLength(500);
                entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(7);
                entity.Property(e => e.RecurrencePattern).HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // ✅ CORRECT - Foreign key relationships using proper property names
                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)  // ✅ Use CreatedBy, NOT CreatedByUserId
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.UpdatedBy)  // ✅ Use UpdatedBy, NOT UpdatedByUserId
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Branch)
                      .WithMany()
                      .HasForeignKey(e => e.BranchId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Indexes for performance
                entity.HasIndex(e => e.StartDate)
                      .HasDatabaseName("IX_CalendarEvents_StartDate");
                
                entity.HasIndex(e => e.EventType)
                      .HasDatabaseName("IX_CalendarEvents_EventType");
                
                entity.HasIndex(e => e.CreatedBy)
                      .HasDatabaseName("IX_CalendarEvents_CreatedBy");
                
                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId })
                      .HasDatabaseName("IX_CalendarEvents_RelatedEntity");
                
                entity.HasIndex(e => e.BranchId)
                      .HasDatabaseName("IX_CalendarEvents_BranchId");
            });

            // ==================== SALE ITEM BATCH CONFIGURATION ==================== //
            modelBuilder.Entity<SaleItemBatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.BatchNumber)
                      .IsRequired()
                      .HasMaxLength(50);
                
                entity.Property(e => e.CostPerUnit)
                      .HasColumnType("decimal(18,2)");
                
                entity.Property(e => e.TotalCost)
                      .HasColumnType("decimal(18,2)");
                
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                
                // Foreign key relationships
                entity.HasOne(e => e.SaleItem)
                      .WithMany()
                      .HasForeignKey(e => e.SaleItemId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Batch)
                      .WithMany()
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Indexes for performance
                entity.HasIndex(e => e.SaleItemId)
                      .HasDatabaseName("IX_SaleItemBatches_SaleItemId");
                
                entity.HasIndex(e => e.BatchId)
                      .HasDatabaseName("IX_SaleItemBatches_BatchId");
                
                entity.HasIndex(e => e.BatchNumber)
                      .HasDatabaseName("IX_SaleItemBatches_BatchNumber");
                
                entity.HasIndex(e => e.CreatedAt)
                      .HasDatabaseName("IX_SaleItemBatches_CreatedAt");
                      
                // Composite index for query optimization
                entity.HasIndex(e => new { e.SaleItemId, e.BatchId })
                      .HasDatabaseName("IX_SaleItemBatches_SaleItem_Batch");
            });
            
            // ==================== MULTI-BRANCH INTEGRATION CONFIGURATIONS ==================== //
            
            // BranchAccess Configuration
            modelBuilder.Entity<BranchAccess>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Foreign key relationships
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.Branch)
                      .WithMany()
                      .HasForeignKey(d => d.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.AssignedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.AssignedBy)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                // Indexes for performance
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_BranchAccess_UserId");
                      
                entity.HasIndex(e => e.BranchId)
                      .HasDatabaseName("IX_BranchAccess_BranchId");
                      
                entity.HasIndex(e => e.IsActive)
                      .HasDatabaseName("IX_BranchAccess_IsActive");
            });
            
            // TransferRequest Configuration
            modelBuilder.Entity<TransferRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Unique transfer number
                entity.HasIndex(e => e.TransferNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_TransferRequest_TransferNumber");
                      
                // Foreign key relationships
                entity.HasOne(d => d.SourceBranch)
                      .WithMany()
                      .HasForeignKey(d => d.SourceBranchId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.TargetBranch)
                      .WithMany()
                      .HasForeignKey(d => d.TargetBranchId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.RequestedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.RequestedBy)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(d => d.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ApprovedBy)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                // Indexes for performance
                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_TransferRequest_Status");
                      
                entity.HasIndex(e => e.Priority)
                      .HasDatabaseName("IX_TransferRequest_Priority");
                      
                entity.HasIndex(e => e.SourceBranchId)
                      .HasDatabaseName("IX_TransferRequest_SourceBranch");
                      
                entity.HasIndex(e => e.TargetBranchId)
                      .HasDatabaseName("IX_TransferRequest_TargetBranch");
                      
                entity.HasIndex(e => e.RequestedAt)
                      .HasDatabaseName("IX_TransferRequest_RequestedAt");
                      
                entity.HasIndex(e => new { e.SourceBranchId, e.Status })
                      .HasDatabaseName("IX_TransferRequest_SourceBranch_Status");
                      
                entity.HasIndex(e => new { e.TargetBranchId, e.Status })
                      .HasDatabaseName("IX_TransferRequest_TargetBranch_Status");
            });
            
            // TransferItem Configuration
            modelBuilder.Entity<TransferItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Foreign key relationships
                entity.HasOne(d => d.TransferRequest)
                      .WithMany(p => p.Items)
                      .HasForeignKey(d => d.TransferRequestId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(d => d.Product)
                      .WithMany()
                      .HasForeignKey(d => d.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Property configurations - use the actual database column properties
                entity.Property(e => e.RequestedQuantity)
                      .HasColumnType("decimal(18,4)")
                      .HasDefaultValue(0);
                
                entity.Property(e => e.UnitPrice)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.TotalPrice)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                // Indexes for performance
                entity.HasIndex(e => e.TransferRequestId)
                      .HasDatabaseName("IX_TransferItem_TransferRequestId");
                      
                entity.HasIndex(e => e.ProductId)
                      .HasDatabaseName("IX_TransferItem_ProductId");
                      
                entity.HasIndex(e => new { e.TransferRequestId, e.ProductId })
                      .IsUnique()
                      .HasDatabaseName("IX_TransferItem_TransferRequest_Product");
            });
        }
    }
}