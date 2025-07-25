// QUICK FIX - Update Program.cs (remove EnableAnnotations line)

using Berca_Backend.Data;
using Berca_Backend.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations; // digunakan untuk validasi model

var builder = WebApplication.CreateBuilder(args);

// Setup koneksi ke SQL Server menggunakan AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ ADD: Configure file upload options untuk UserProfile foto
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024; // 500KB limit untuk foto
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
});

// Daftarkan AuthService ke DI container untuk logika login/register
builder.Services.AddScoped<IAuthService, AuthService>();

// Konfigurasi cookie-based authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        // 🔧 Critical Cookie Settings for CORS
        options.Cookie.Name = ".AspNetCore.Cookies";
        options.Cookie.HttpOnly = false; // Allow JS access for debugging
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // HTTP for development
        options.Cookie.SameSite = SameSiteMode.Lax; // Allow cross-origin
        options.Cookie.Domain = null; // Auto-detect domain
        options.Cookie.Path = "/";
        options.Cookie.MaxAge = TimeSpan.FromMinutes(30);

        // 🔧 Important: Return JSON responses instead of redirects
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

// Tambahkan sistem otorisasi
builder.Services.AddAuthorization();

// Daftarkan controller dan Swagger (API docs)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Toko Eniwan POS API",
        Version = "v1",
        Description = "API untuk sistem Point of Sale Toko Eniwan"
    });

    // ✅ REMOVED: EnableAnnotations() - not needed for basic functionality
});

// Setup CORS agar frontend (Angular) bisa akses backend (ASP.NET Core)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // 🔑 CRITICAL for cookies
              .SetIsOriginAllowed(origin =>
              {
                  Console.WriteLine($"🌐 CORS Origin: {origin}");
                  return origin.StartsWith("http://localhost");
              });
    });
});

var app = builder.Build();

// ✅ ADD: Enable static files untuk serving uploaded images
app.UseStaticFiles();

// Aktifkan Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Toko Eniwan POS API v1");
    c.RoutePrefix = "swagger"; // Swagger UI akan available di /swagger
});

// Middleware yang dijalankan secara berurutan
app.UseCors();            // Izinkan request dari Angular
app.UseCookiePolicy();    // Kelola kebijakan cookie
app.UseAuthentication();  // Periksa autentikasi (cookie)
app.UseAuthorization();   // Cek hak akses (kalau pakai [Authorize])

// ✅ ADD: Auto-create uploads directory dan database check
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        // Check database connection
        var canConnect = context.Database.CanConnect();
        Console.WriteLine($"🔗 Database connection: {canConnect}");

        if (canConnect)
        {
            var userCount = context.Users.Count();
            Console.WriteLine($"👥 Users in database: {userCount}");

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? app.Environment.ContentRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsPath);
            Console.WriteLine($"📁 Uploads directory: {uploadsPath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database warning: {ex.Message}");
    }
}

// Aktifkan routing controller
app.MapControllers();

// ✅ ADD: Log important URLs on startup
Console.WriteLine("🚀 Toko Eniwan POS API Starting...");
Console.WriteLine($"📍 API Base URL: http://localhost:5106");
Console.WriteLine($"📖 Swagger UI: http://localhost:5106/swagger");
Console.WriteLine($"🔐 Auth endpoints: http://localhost:5106/auth/login");
Console.WriteLine($"👤 Profile endpoint: http://localhost:5106/api/UserProfile");

// Jalankan aplikasi
app.Run();