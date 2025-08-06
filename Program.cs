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

// ✅ FIXED CORS - Remove invalid method
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",     // Angular dev server
                "http://localhost:5173",     // Vite dev server  
                "https://localhost:4200",    // HTTPS variants
                "https://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();          // ✅ Enable credentials for cookies
        // ✅ REMOVED: SetIsOriginAllowedToReturnTrue() - this method doesn't exist
    });
});

// ✅ IMPROVED Cookie Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // ✅ DEVELOPMENT-FRIENDLY COOKIE SETTINGS
        options.Cookie.Name = "TokoEniwanAuth";
        options.Cookie.HttpOnly = false;          // ✅ Allow JS access for debugging
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // ✅ Allow HTTP in dev
        options.Cookie.SameSite = SameSiteMode.Lax;     // ✅ Lax for cross-origin
        options.Cookie.Domain = null;             // ✅ Don't set domain in dev
        options.Cookie.Path = "/";                // ✅ Root path
        options.Cookie.MaxAge = TimeSpan.FromHours(8);
        options.Cookie.IsEssential = true;        // ✅ Essential for functionality

        // ✅ API-FRIENDLY EVENT HANDLERS
        options.Events.OnRedirectToLogin = context =>
        {
            // Don't redirect API calls to login page, return 401 instead
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
            // Don't redirect API calls, return 403 instead
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// ✅ Policy-Based Authorization (keep existing configuration)
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
builder.Services.AddScoped<IDashboardService, DashboardService>();
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

// ✅ CRITICAL: Middleware pipeline order (VERY IMPORTANT!)
app.UseCors();              // ✅ 1. CORS first
app.UseCookiePolicy();      // ✅ 2. Cookie policy
app.UseAuthentication();    // ✅ 3. Authentication
app.UseAuthorization();     // ✅ 4. Authorization

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