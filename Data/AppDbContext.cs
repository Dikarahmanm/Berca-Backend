// Data/AppDbContext.cs - Updated
using Berca_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Existing DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<LogActivity> LogActivities { get; set; }

        // ✅ NEW: Category DbSet
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Existing UserProfile configuration
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

            // ✅ NEW: Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Color)
                      .IsRequired()
                      .HasMaxLength(7); // #FFFFFF format

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Unique constraint for category name
                entity.HasIndex(e => e.Name)
                      .IsUnique()
                      .HasDatabaseName("IX_Categories_Name");

                // Index for color filtering
                entity.HasIndex(e => e.Color)
                      .HasDatabaseName("IX_Categories_Color");
            });

            // Existing LogActivity configuration
            modelBuilder.Entity<LogActivity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });

            // ✅ Seed default categories (FIXED: Static DateTime values)
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Makanan",
                    Color = "#FF914D", // Orange primary
                    Description = "Produk makanan dan snack",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 2,
                    Name = "Minuman",
                    Color = "#4BBF7B", // Green accent
                    Description = "Minuman segar dan berenergi",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 3,
                    Name = "Elektronik",
                    Color = "#E15A4F", // Red error
                    Description = "Perangkat elektronik dan aksesoris",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 4,
                    Name = "Rumah Tangga",
                    Color = "#FFB84D", // Yellow warning
                    Description = "Keperluan dan peralatan rumah tangga",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 5,
                    Name = "Kesehatan",
                    Color = "#6366F1", // Indigo
                    Description = "Produk kesehatan dan perawatan",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}