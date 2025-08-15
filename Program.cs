using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Berca_Backend.Data;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Add controllers
builder.Services.AddControllers();

// ✅ CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:5173",
                "https://localhost:4200",
                "https://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ✅ Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "TokoEniwanAuth";
        options.Cookie.HttpOnly = false;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Domain = null;
        options.Cookie.Path = "/";
        options.Cookie.MaxAge = TimeSpan.FromHours(8);
        options.Cookie.IsEssential = true;

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// ==========================================
// ✅ ENHANCED AUTHORIZATION POLICIES - Branch-Aware System
// ==========================================

builder.Services.AddAuthorization(options =>
{
    // ===== GLOBAL ACCESS POLICIES ===== //

    // Global system access - Admin only
    options.AddPolicy("Global.Access", policy =>
        policy.RequireRole("Admin"));

    // Multi-branch management - Admin + HeadManager
    options.AddPolicy("MultiBranch.Access", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    // ===== BRANCH MANAGEMENT POLICIES ===== //

    // Branch CRUD operations
    options.AddPolicy("Branch.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    options.AddPolicy("Branch.Write", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    options.AddPolicy("Branch.Manage", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Branch user assignment
    options.AddPolicy("Branch.AssignUsers", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    // ===== CONSOLIDATED REPORTING POLICIES ===== //

    // Consolidated reports access - Manager levels only
    options.AddPolicy("Reports.Consolidated", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Cross-branch data access
    options.AddPolicy("Reports.CrossBranch", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    // Branch-specific reports
    options.AddPolicy("Reports.Branch", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "User"));

    // ===== ENHANCED EXISTING POLICIES (Branch-Aware) ===== //

    // Dashboard Policies (Branch-aware)
    options.AddPolicy("Dashboard.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Dashboard.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // NEW: Consolidated Dashboard (Manager-only)
    options.AddPolicy("Dashboard.Consolidated", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // POS Policies (Branch-specific)
    options.AddPolicy("POS.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User", "Cashier"));

    options.AddPolicy("POS.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User", "Cashier"));

    options.AddPolicy("POS.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Inventory Policies (Branch-aware)
    options.AddPolicy("Inventory.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Inventory.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Inventory.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // NEW: Cross-branch inventory transfer
    options.AddPolicy("Inventory.Transfer", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Category Policies
    options.AddPolicy("Category.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Category.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("Category.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "Manager"));

    // Membership Policies
    options.AddPolicy("Membership.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Membership.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Membership.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // User Management Policies (Branch-specific)
    options.AddPolicy("UserManagement.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("UserManagement.Write", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    options.AddPolicy("UserManagement.Delete", policy =>
        policy.RequireRole("Admin"));

    // Reports Policies (Enhanced)
    options.AddPolicy("Reports.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("Reports.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Notifications Policies
    options.AddPolicy("Notifications.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Notifications.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Admin-only policies
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));

    // ===== CUSTOM BRANCH ACCESS VALIDATION ===== //

    // Branch-specific data access validation
    options.AddPolicy("Branch.DataAccess", policy =>
        policy.RequireAssertion(context =>
        {
            return ValidateBranchDataAccess(context);
        }));

    // Branch hierarchy access validation
    options.AddPolicy("Branch.HierarchyAccess", policy =>
        policy.RequireAssertion(context =>
        {
            return ValidateBranchHierarchyAccess(context);
        }));
});

// ===== CUSTOM AUTHORIZATION HELPER FUNCTIONS ===== //

// Helper function untuk validasi branch access
static bool ValidateBranchDataAccess(AuthorizationHandlerContext context)
{
    var user = context.User;
    var role = user.FindFirst(ClaimTypes.Role)?.Value;

    // Admin bisa akses semua
    if (role == "Admin") return true;

    // Untuk role lain, perlu validasi branch assignment
    var userBranchId = user.FindFirst("BranchId")?.Value;
    var canAccessMultiple = user.FindFirst("CanAccessMultipleBranches")?.Value == "true";
    var accessibleBranches = user.FindFirst("AccessibleBranchIds")?.Value;

    // Extract requested branch ID dari HTTP context
    if (context.Resource is DefaultHttpContext httpContext)
    {
        var requestedBranchId = httpContext.Request.Query["branchId"].ToString();

        if (!string.IsNullOrEmpty(requestedBranchId) && int.TryParse(requestedBranchId, out int branchId))
        {
            // User assigned ke branch ini
            if (userBranchId == requestedBranchId) return true;

            // User punya multi-branch access
            if (canAccessMultiple && !string.IsNullOrEmpty(accessibleBranches))
            {
                try
                {
                    var branchIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(accessibleBranches);
                    return branchIds?.Contains(branchId) == true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    return false;
}

// Helper function untuk validasi branch hierarchy access
static bool ValidateBranchHierarchyAccess(AuthorizationHandlerContext context)
{
    var user = context.User;
    var role = user.FindFirst(ClaimTypes.Role)?.Value;

    // Admin dan HeadManager bisa akses hierarki penuh
    if (role == "Admin" || role == "HeadManager") return true;

    // BranchManager bisa akses branch sendiri + sub-branches
    if (role == "BranchManager") return true;

    // Role lain tidak bisa akses hierarki
    return false;
}

// ✅ Register services in correct dependency order
builder.Services.AddScoped<ITimezoneService, TimezoneService>(); // FIRST - other services depend on this
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IPOSService, POSService>();
builder.Services.AddScoped<IUserBranchAssignmentService, UserBranchAssignmentService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IConsolidatedReportService, ConsolidatedReportService>();

// ✅ Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Toko Eniwan POS API",
        Version = "v1",
        Description = "Enhanced with Multi-Branch System - POS, Inventory, Membership, Notifications & Dashboard APIs"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "TokoEniwanAuth",
        Description = "Cookie-based authentication with branch-aware authorization"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Cookie"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ✅ Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ✅ Add cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

var app = builder.Build();

// ✅ FIXED: Create directories BEFORE configuring static files
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");
var avatarsPath = Path.Combine(uploadsPath, "avatars");
var exportsPath = Path.Combine(wwwrootPath, "exports");

// Ensure all directories exist
Directory.CreateDirectory(wwwrootPath);
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(avatarsPath);
Directory.CreateDirectory(exportsPath);

// Create export subdirectories
Directory.CreateDirectory(Path.Combine(exportsPath, "sales"));
Directory.CreateDirectory(Path.Combine(exportsPath, "inventory"));
Directory.CreateDirectory(Path.Combine(exportsPath, "financial"));
Directory.CreateDirectory(Path.Combine(exportsPath, "customer"));

// ✅ Enable default static files
app.UseStaticFiles();

// ✅ Static files for avatar uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ✅ Static files for exports
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(exportsPath),
    RequestPath = "/exports"
});

// ✅ Enable Swagger in development and staging
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Toko Eniwan POS API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

// ✅ Middleware pipeline order
app.UseCors();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Map controllers
app.MapControllers();

// ✅ Global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var contextLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        contextLogger.LogError("Unhandled exception occurred");

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            success = false,
            message = "An internal server error occurred",
            timestamp = DateTime.UtcNow
        }));
    });
});

// ✅ Log startup information
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 Toko Eniwan POS API Starting...");
startupLogger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("📍 API Base URL: http://localhost:5171");
startupLogger.LogInformation("🏢 Multi-Branch System: ENABLED");

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    startupLogger.LogInformation("📖 Swagger UI: http://localhost:5171/swagger");
}

startupLogger.LogInformation("🔐 Branch-Aware Authentication Configured");
startupLogger.LogInformation("📁 Directories Created: wwwroot, uploads, exports");

// ✅ Auto-setup database and sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("📊 Database ensured/created successfully");

        // Seed original sample data first
        await SampleDataSeeder.SeedSampleDataAsync(context);
        logger.LogInformation("🌱 Original sample data seeded successfully");

        // Then seed branch sample data
        await SeedBranchSampleData(context, logger);
        logger.LogInformation("🏢 Branch sample data seeded successfully");

        // Fix any existing users with NULL branch assignments
        await SampleDataSeeder.FixNullBranchAssignmentsAsync(context);
        logger.LogInformation("🔧 Fixed NULL branch assignments for existing users");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database setup or seeding");
    }
}

// ✅ Run the application
await app.RunAsync();

// ===== SAMPLE DATA SEEDER FOR BRANCHES ===== //

static async Task SeedBranchSampleData(AppDbContext context, ILogger logger)
{
    // Check if branches already exist
    if (await context.Branches.AnyAsync())
    {
        logger.LogInformation("🏢 Branch data already exists, skipping seed");
        return;
    }

    logger.LogInformation("🌱 Seeding retail chain branch data...");

    // Use TimezoneService for proper Jakarta time
    var timezoneService = new Berca_Backend.Services.TimezoneService(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<Berca_Backend.Services.TimezoneService>.Instance);

    var now = timezoneService.Now;
    var utcNow = DateTime.UtcNow;

    var branches = new List<Berca_Backend.Models.Branch>
    {
        // Head Office
        new Berca_Backend.Models.Branch
        {
            BranchCode = "HQ",
            BranchName = "Toko Eniwan Head Office",
            ParentBranchId = null,
            BranchType = Berca_Backend.Models.BranchType.Head,
            Address = "Jl. Merdeka No. 123, Jakarta Pusat, DKI Jakarta",
            ManagerName = "Maharaja Dika",
            Phone = "021-1234567",
            Email = "admin@tokoeniwan.com",
            City = "Jakarta",
            Province = "DKI Jakarta",
            PostalCode = "10110",
            OpeningDate = timezoneService.LocalToUtc(now.AddYears(-3)), // UTC for database
            StoreSize = "Large",
            EmployeeCount = 25,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        
        // Retail Chain Branches
        new Berca_Backend.Models.Branch
        {
            BranchCode = "PWK001",
            BranchName = "Toko Eniwan Purwakarta",
            ParentBranchId = null, // Flat structure
            BranchType = Berca_Backend.Models.BranchType.Branch,
            Address = "Jl. Ahmad Yani No. 45, Purwakarta, Jawa Barat",
            ManagerName = "Budi Santoso",
            Phone = "0264-123456",
            Email = "purwakarta@tokoeniwan.com",
            City = "Purwakarta",
            Province = "Jawa Barat",
            PostalCode = "41115",
            OpeningDate = timezoneService.LocalToUtc(now.AddYears(-2)),
            StoreSize = "Medium",
            EmployeeCount = 8,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        new Berca_Backend.Models.Branch
        {
            BranchCode = "BDG001",
            BranchName = "Toko Eniwan Bandung",
            ParentBranchId = null,
            BranchType = Berca_Backend.Models.BranchType.Branch,
            Address = "Jl. Cihampelas No. 120, Bandung, Jawa Barat",
            ManagerName = "Sari Indrawati",
            Phone = "022-987654",
            Email = "bandung@tokoeniwan.com",
            City = "Bandung",
            Province = "Jawa Barat",
            PostalCode = "40131",
            OpeningDate = timezoneService.LocalToUtc(now.AddYears(-1)),
            StoreSize = "Large",
            EmployeeCount = 15,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        new Berca_Backend.Models.Branch
        {
            BranchCode = "SBY001",
            BranchName = "Toko Eniwan Surabaya",
            ParentBranchId = null,
            BranchType = Berca_Backend.Models.BranchType.Branch,
            Address = "Jl. Raya Darmo No. 88, Surabaya, Jawa Timur",
            ManagerName = "Ahmad Hidayat",
            Phone = "031-567890",
            Email = "surabaya@tokoeniwan.com",
            City = "Surabaya",
            Province = "Jawa Timur",
            PostalCode = "60265",
            OpeningDate = timezoneService.LocalToUtc(now.AddMonths(-8)),
            StoreSize = "Large",
            EmployeeCount = 12,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        new Berca_Backend.Models.Branch
        {
            BranchCode = "BKS001",
            BranchName = "Toko Eniwan Bekasi",
            ParentBranchId = null,
            BranchType = Berca_Backend.Models.BranchType.Branch,
            Address = "Jl. Cut Meutia No. 99, Bekasi, Jawa Barat",
            ManagerName = "Dewi Lestari",
            Phone = "021-8888999",
            Email = "bekasi@tokoeniwan.com",
            City = "Bekasi",
            Province = "Jawa Barat",
            PostalCode = "17112",
            OpeningDate = timezoneService.LocalToUtc(now.AddMonths(-6)),
            StoreSize = "Medium",
            EmployeeCount = 10,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        new Berca_Backend.Models.Branch
        {
            BranchCode = "BGR001",
            BranchName = "Toko Eniwan Bogor",
            ParentBranchId = null,
            BranchType = Berca_Backend.Models.BranchType.Branch,
            Address = "Jl. Pajajaran No. 77, Bogor, Jawa Barat",
            ManagerName = "Rini Setiawan",
            Phone = "0251-333444",
            Email = "bogor@tokoeniwan.com",
            City = "Bogor",
            Province = "Jawa Barat",
            PostalCode = "16129",
            OpeningDate = timezoneService.LocalToUtc(now.AddMonths(-3)),
            StoreSize = "Small",
            EmployeeCount = 6,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        }
    };

    context.Branches.AddRange(branches);
    await context.SaveChangesAsync();

    logger.LogInformation("✅ Retail chain sample data seeded: {Count} branches created", branches.Count);
    logger.LogInformation("🏪 Branches: Head Office + 5 retail locations (Purwakarta, Bandung, Surabaya, Bekasi, Bogor)");
    logger.LogInformation("🕐 Opening dates calculated using Asia/Jakarta timezone");

    // Update existing users with realistic branch assignments
    await AssignUsersToRetailBranches(context, logger);
}

static async Task AssignUsersToRetailBranches(AppDbContext context, ILogger logger)
{
    var users = await context.Users.ToListAsync();

    if (!users.Any())
    {
        logger.LogInformation("👥 No users found, skipping branch assignment");
        return;
    }

    logger.LogInformation("👥 Assigning users to retail branches...");

    foreach (var user in users)
    {
        switch (user.Role)
        {
            case "Admin":
                // Admin global access - bisa akses semua cabang
                user.BranchId = null; // Head Office management
                user.CanAccessMultipleBranches = true;
                user.SetAccessibleBranchIds(new List<int> { 1, 2, 3, 4, 5, 6 }); // All branches
                break;

            case "HeadManager":
                // Regional manager - multiple branches
                user.BranchId = 1; // Head Office
                user.CanAccessMultipleBranches = true;
                user.SetAccessibleBranchIds(new List<int> { 1, 2, 3, 4 }); // Head + Jabar region
                break;

            case "BranchManager":
            case "Manager": // Support existing role
                // Single branch manager
                user.BranchId = 2; // Default to Purwakarta branch
                user.CanAccessMultipleBranches = false;
                user.SetAccessibleBranchIds(new List<int> { 2 }); // Only their branch
                break;

            case "User":
                // Store staff - assigned to specific branch
                user.BranchId = 3; // Default to Bandung branch
                user.CanAccessMultipleBranches = false;
                user.SetAccessibleBranchIds(new List<int>()); // No additional access
                break;

            case "Cashier":
                // Cashier - specific branch only
                user.BranchId = 2; // Default to Purwakarta branch
                user.CanAccessMultipleBranches = false;
                user.SetAccessibleBranchIds(new List<int>()); // No additional access
                break;
        }
    }

    await context.SaveChangesAsync();
    logger.LogInformation("✅ Users assigned to retail branches successfully");
    logger.LogInformation("🏪 Branch assignments: Admin (Global), HeadManager (Multi), BranchManager (Single), Staff (Local)");
}