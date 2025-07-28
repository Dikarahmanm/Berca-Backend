// Program.cs - Fixed Program.cs with Corrected Logger Issue
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Berca_Backend.Data;
using Berca_Backend.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ Add Entity Framework dengan connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Add controllers
builder.Services.AddControllers();

// ✅ Add CORS policy untuk Angular development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5173") // Angular dev servers
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Penting untuk cookie-based auth
    });
});

// ✅ Cookie-based Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // 8 hours
        options.SlidingExpiration = true;
        options.Cookie.Name = "TokoEniwanAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// ✅ Policy-Based Authorization
builder.Services.AddAuthorization(options =>
{
    // Dashboard Policies
    options.AddPolicy("Dashboard.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Dashboard.Write", policy =>
        policy.RequireRole("Admin", "Manager"));

    // POS Policies
    options.AddPolicy("POS.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("POS.Write", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("POS.Delete", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Inventory Policies
    options.AddPolicy("Inventory.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Inventory.Write", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Inventory.Delete", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Category Policies
    options.AddPolicy("Category.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Category.Write", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("Category.Delete", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Membership Policies
    options.AddPolicy("Membership.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Membership.Write", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Membership.Delete", policy =>
        policy.RequireRole("Admin", "Manager"));

    // User Management Policies
    options.AddPolicy("UserManagement.Read", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("UserManagement.Write", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("UserManagement.Delete", policy =>
        policy.RequireRole("Admin"));

    // Reports Policies
    options.AddPolicy("Reports.Read", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("Reports.Write", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Notifications Policies
    options.AddPolicy("Notifications.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Notifications.Write", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Admin-only policies
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));
});

// ✅ Register Sprint 2 Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPOSService, POSService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>(); // ✅ Fixed: DashboardService now exists
builder.Services.AddScoped<ICategoryService, CategoryService>();

// ✅ Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Toko Eniwan POS API",
        Version = "v1",
        Description = "Sprint 2 - POS, Inventory, Membership, Notifications & Dashboard APIs"
    });

    // Include XML comments for better documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for cookie authentication
    c.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "TokoEniwanAuth",
        Description = "Cookie-based authentication"
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
    options.CheckConsentNeeded = context => false; // Disable consent for API
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

var app = builder.Build();

// ✅ Enable static files untuk serving uploaded images
app.UseStaticFiles();

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

// ✅ Middleware pipeline (order matters!)
app.UseCors();            // CORS harus sebelum Authentication
app.UseCookiePolicy();    // Cookie policy
app.UseAuthentication();  // Authentication harus sebelum Authorization
app.UseAuthorization();   // Authorization

// ✅ Auto-setup database dan directories
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var appLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>(); // ✅ Fixed: renamed to avoid conflict

    try
    {
        // ✅ Auto-migrate database
        await context.Database.MigrateAsync();
        appLogger.LogInformation("✅ Database migration completed successfully");

        // Check database connection
        var canConnect = await context.Database.CanConnectAsync();
        appLogger.LogInformation("🔗 Database connection: {CanConnect}", canConnect);

        if (canConnect)
        {
            // Log table counts
            var userCount = await context.Users.CountAsync();
            var categoryCount = await context.Categories.CountAsync();
            var productCount = await context.Products.CountAsync();
            var memberCount = await context.Members.CountAsync();
            var saleCount = await context.Sales.CountAsync();
            var notificationCount = await context.Notifications.CountAsync();

            appLogger.LogInformation("📊 Database Statistics:");
            appLogger.LogInformation("   👥 Users: {UserCount}", userCount);
            appLogger.LogInformation("   📂 Categories: {CategoryCount}", categoryCount);
            appLogger.LogInformation("   📦 Products: {ProductCount}", productCount);
            appLogger.LogInformation("   🎫 Members: {MemberCount}", memberCount);
            appLogger.LogInformation("   💰 Sales: {SaleCount}", saleCount);
            appLogger.LogInformation("   🔔 Notifications: {NotificationCount}", notificationCount);

            // Create uploads directories
            var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? app.Environment.ContentRootPath, "uploads");
            var avatarsPath = Path.Combine(uploadsPath, "avatars");
            var receiptsPath = Path.Combine(uploadsPath, "receipts");

            Directory.CreateDirectory(avatarsPath);
            Directory.CreateDirectory(receiptsPath);

            appLogger.LogInformation("📁 Upload directories created:");
            appLogger.LogInformation("   📸 Avatars: {AvatarsPath}", avatarsPath);
            appLogger.LogInformation("   🧾 Receipts: {ReceiptsPath}", receiptsPath);
        }
    }
    catch (Exception ex)
    {
        appLogger.LogError(ex, "⚠️ Database setup error: {Message}", ex.Message);
    }
}

// ✅ Map controllers
app.MapControllers();

// ✅ Global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var contextLogger = context.RequestServices.GetRequiredService<ILogger<Program>>(); // ✅ Fixed: avoid conflict
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
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>(); // ✅ Fixed: avoid conflict
startupLogger.LogInformation("🚀 Toko Eniwan POS API Starting...");
startupLogger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("📍 API Base URL: http://localhost:5171");

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    startupLogger.LogInformation("📖 Swagger UI: http://localhost:5171/swagger");
}

startupLogger.LogInformation("🔐 Available Endpoints:");
startupLogger.LogInformation("   🔑 Auth: http://localhost:5171/auth/*");
startupLogger.LogInformation("   👤 Profile: http://localhost:5171/api/UserProfile");
startupLogger.LogInformation("   📂 Categories: http://localhost:5171/api/Category");
startupLogger.LogInformation("   📦 Products: http://localhost:5171/api/Product");
startupLogger.LogInformation("   💰 POS: http://localhost:5171/api/POS");
startupLogger.LogInformation("   🎫 Members: http://localhost:5171/api/Member");
startupLogger.LogInformation("   🔔 Notifications: http://localhost:5171/api/Notification");
startupLogger.LogInformation("   📊 Dashboard: http://localhost:5171/api/Dashboard");

startupLogger.LogInformation("✨ Sprint 2 Services Registered:");
startupLogger.LogInformation("   ✅ Product Service - Inventory management");
startupLogger.LogInformation("   ✅ POS Service - Point of sale transactions");
startupLogger.LogInformation("   ✅ Member Service - Membership & loyalty points");
startupLogger.LogInformation("   ✅ Notification Service - Real-time notifications");
startupLogger.LogInformation("   ✅ Dashboard Service - Analytics & reports");
startupLogger.LogInformation("   ✅ Category Service - Product categorization");

// ✅ Run the application
await app.RunAsync();