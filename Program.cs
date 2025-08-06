using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Berca_Backend.Data;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces; // ✅ ADDED
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Add controllers
builder.Services.AddControllers();

// ✅ CORS Configuration (existing)
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

// ✅ Authentication (existing)
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

// ✅ Authorization (existing)
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

// ✅ CRITICAL: Register services in correct dependency order
builder.Services.AddScoped<ITimezoneService, TimezoneService>(); // ✅ FIRST

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>(); // ✅ BEFORE POSService
builder.Services.AddScoped<IMemberService, MemberService>();

// ✅ POSService now depends on INotificationService too
builder.Services.AddScoped<IPOSService, POSService>(); // Depends on IProductService, IMemberService, ITimezoneService, INotificationService

// ✅ Add Swagger
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

// ✅ Enable static files
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

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    startupLogger.LogInformation("📖 Swagger UI: http://localhost:5171/swagger");
}

startupLogger.LogInformation("🔐 Cookie Authentication Configured:");
startupLogger.LogInformation("   🍪 Cookie Name: TokoEniwanAuth");
startupLogger.LogInformation("   🕐 Expiry: 8 hours");
startupLogger.LogInformation("   🔒 HttpOnly: false (dev mode)");
startupLogger.LogInformation("   🌐 SameSite: Lax");

// ✅ Auto-setup database and sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("📊 Database ensured/created successfully");
        
        // Seed sample data for dashboard demo
        await SampleDataSeeder.SeedSampleDataAsync(context);
        logger.LogInformation("🌱 Sample data seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database setup or seeding");
    }
}

// ✅ Run the application
await app.RunAsync();