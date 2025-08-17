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

    // ===== INVENTORY TRANSFER POLICIES ===== //

    // Transfer request creation
    options.AddPolicy("Transfer.Create", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Transfer approval - HeadManager for high-value, BranchManager for low-value
    options.AddPolicy("Transfer.Approve", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Transfer view access
    options.AddPolicy("Transfer.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Transfer workflow management (ship, receive, cancel)
    options.AddPolicy("Transfer.Manage", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Emergency transfer creation
    options.AddPolicy("Transfer.Emergency", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Transfer analytics and reporting
    options.AddPolicy("Transfer.Analytics", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // ===== SUPPLIER MANAGEMENT POLICIES ===== //

    // Supplier CRUD operations
    options.AddPolicy("Supplier.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Supplier.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("Supplier.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

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

// ✅ Add Memory Cache for ConsolidatedReportService
builder.Services.AddMemoryCache();

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
builder.Services.AddScoped<IInventoryTransferService, InventoryTransferService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

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

        // Seed supplier sample data
        await SeedSupplierSampleData(context, logger);
        logger.LogInformation("🏪 Supplier sample data seeded successfully");
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

// ===== SUPPLIER SAMPLE DATA SEEDER ===== //

static async Task SeedSupplierSampleData(AppDbContext context, ILogger logger)
{
    // Check if suppliers already exist
    if (await context.Suppliers.AnyAsync())
    {
        logger.LogInformation("🏪 Supplier data already exists, skipping seed");
        return;
    }

    logger.LogInformation("🌱 Seeding supplier sample data...");

    // Use TimezoneService for proper Jakarta time
    var timezoneService = new Berca_Backend.Services.TimezoneService(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<Berca_Backend.Services.TimezoneService>.Instance);

    var now = timezoneService.Now;
    var utcNow = DateTime.UtcNow;

    var suppliers = new List<Berca_Backend.Models.Supplier>
    {
        // Global suppliers (available to all branches)
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00001",
            CompanyName = "PT. Sumber Rejeki Makmur",
            ContactPerson = "Bambang Sutrisno",
            Phone = "021-5551234",
            Email = "bambang@sumberrejeki.co.id",
            Address = "Jl. Industri Raya No. 88, Jakarta Timur, DKI Jakarta 13560",
            PaymentTerms = 30,
            CreditLimit = 500000000, // 500M IDR
            BranchId = null, // Global supplier
            IsActive = true,
            CreatedBy = 5, // dikdika user
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00002",
            CompanyName = "CV. Mitra Elektronik Nusantara",
            ContactPerson = "Siti Nurhaliza",
            Phone = "022-8887777",
            Email = "siti@mitraelektronik.com",
            Address = "Jl. Soekarno-Hatta No. 456, Bandung, Jawa Barat 40286",
            PaymentTerms = 45,
            CreditLimit = 750000000, // 750M IDR
            BranchId = null, // Global supplier
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00003",
            CompanyName = "PT. Indo Komputer Teknologi",
            ContactPerson = "Ahmad Rahman",
            Phone = "031-9998888",
            Email = "ahmad@indokomputer.co.id",
            Address = "Jl. Basuki Rahmat No. 77, Surabaya, Jawa Timur 60271",
            PaymentTerms = 60,
            CreditLimit = 1000000000, // 1B IDR
            BranchId = null, // Global supplier
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        // Branch-specific suppliers
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00004",
            CompanyName = "Toko Kelontong Pak Hasan",
            ContactPerson = "Hasan Basri",
            Phone = "0264-555111",
            Email = "hasan@tokokelontong.com",
            Address = "Jl. Veteran No. 12, Purwakarta, Jawa Barat 41118",
            PaymentTerms = 7, // Short term
            CreditLimit = 25000000, // 25M IDR
            BranchId = 2, // Purwakarta branch
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00005",
            CompanyName = "CV. Bandung Fresh Market",
            ContactPerson = "Rina Marlina",
            Phone = "022-4445555",
            Email = "rina@bandungfresh.co.id",
            Address = "Jl. Pasteur No. 99, Bandung, Jawa Barat 40161",
            PaymentTerms = 14,
            CreditLimit = 150000000, // 150M IDR
            BranchId = 3, // Bandung branch
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00006",
            CompanyName = "PT. Surabaya Grosir Sentral",
            ContactPerson = "Budi Rahardjo",
            Phone = "031-7776666",
            Email = "budi@surabayagrosir.com",
            Address = "Jl. Pasar Besar No. 123, Surabaya, Jawa Timur 60174",
            PaymentTerms = 21,
            CreditLimit = 300000000, // 300M IDR
            BranchId = 4, // Surabaya branch
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00007",
            CompanyName = "Warung Sembako Bu Sari",
            ContactPerson = "Sari Wulandari",
            Phone = "021-6667777",
            Email = "sari@warungsembako.com",
            Address = "Jl. Kalimalang No. 45, Bekasi, Jawa Barat 17112",
            PaymentTerms = 10,
            CreditLimit = 50000000, // 50M IDR
            BranchId = 5, // Bekasi branch
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00008",
            CompanyName = "CV. Bogor Agro Mandiri",
            ContactPerson = "Agus Setiawan",
            Phone = "0251-888999",
            Email = "agus@bogoragro.co.id",
            Address = "Jl. Raya Bogor No. 567, Bogor, Jawa Barat 16129",
            PaymentTerms = 14,
            CreditLimit = 75000000, // 75M IDR
            BranchId = 6, // Bogor branch
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        // Long payment terms supplier (for alerts testing)
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00009",
            CompanyName = "PT. Kredit Panjang International",
            ContactPerson = "Robert Tanoto",
            Phone = "021-3334444",
            Email = "robert@kreditpanjang.com",
            Address = "Jl. Sudirman No. 999, Jakarta Pusat, DKI Jakarta 10220",
            PaymentTerms = 120, // Long term - will trigger alert
            CreditLimit = 2000000000, // 2B IDR - will trigger alert
            BranchId = null,
            IsActive = true,
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        },

        // Inactive supplier (for testing)
        new Berca_Backend.Models.Supplier
        {
            SupplierCode = "SUP-2025-00010",
            CompanyName = "CV. Supplier Tidak Aktif",
            ContactPerson = "Inactive User",
            Phone = "021-0000000",
            Email = "inactive@supplier.com",
            Address = "Jl. Tidak Aktif No. 0, Jakarta, DKI Jakarta 10000",
            PaymentTerms = 30,
            CreditLimit = 100000000,
            BranchId = null,
            IsActive = false, // Inactive - will trigger alert
            CreatedBy = 5,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        }
    };

    context.Suppliers.AddRange(suppliers);
    await context.SaveChangesAsync();

    logger.LogInformation("✅ Supplier sample data seeded: {Count} suppliers created", suppliers.Count);
    logger.LogInformation("🏪 Suppliers: 3 global + 6 branch-specific + 1 long-term + 1 inactive");
    logger.LogInformation("📊 Payment terms: 7-120 days, Credit limits: 25M-2B IDR");
    logger.LogInformation("⚠️ Alert triggers: Long payment terms (120 days), High credit (2B), Inactive status");
}