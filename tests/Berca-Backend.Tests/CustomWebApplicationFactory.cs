using System.Linq;
using Berca_Backend.Data;
using Berca_Backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Berca_Backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with InMemory: remove existing registrations
            var dbOptionDescriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var d in dbOptionDescriptors) services.Remove(d);

            var dbContextDescriptors = services.Where(d => d.ServiceType == typeof(AppDbContext)).ToList();
            foreach (var d in dbContextDescriptors) services.Remove(d);

            // Use isolated EF service provider to avoid provider collisions
            var efSp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_Facture");
                options.UseInternalServiceProvider(efSp);
            });

            // Override authentication to use test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            // Remove hosted background services that may interfere
            var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hs in hostedServices)
            {
                services.Remove(hs);
            }

            // Build provider to run seeding
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed a default admin user for access checks (UserId = 100)
            if (!db.Users.Any(u => u.Id == 100))
            {
                db.Users.Add(new User
                {
                    Id = 100,
                    Username = "testadmin",
                    Name = "Test Admin",
                    PasswordHash = "x",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }

            // Optional: Ensure at least one active branch exists for queries
            if (!db.Branches.Any())
            {
                db.Branches.Add(new Branch
                {
                    BranchName = "HQ",
                    BranchCode = "HQ",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }

            // Ensure the test admin is assigned to an active branch so access checks pass
            var anyBranchId = db.Branches.Select(b => b.Id).First();
            var admin = db.Users.First(u => u.Id == 100);
            if (admin.BranchId != anyBranchId)
            {
                admin.BranchId = anyBranchId;
                db.SaveChanges();
            }

            // Keep seeding minimal to avoid collisions with Program sample data
        });
    }

    // No additional helpers
}
