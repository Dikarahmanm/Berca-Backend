// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Models;

namespace Berca_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<LogActivity> LogActivities { get; set; }

    }
}