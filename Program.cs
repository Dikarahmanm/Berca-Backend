
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Berca API", Version = "v1" });
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

// Aktifkan Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Berca API v1");
});

// Middleware yang dijalankan secara berurutan
app.UseCors();            // Izinkan request dari Angular
app.UseCookiePolicy();    // Kelola kebijakan cookie
app.UseAuthentication();  // Periksa autentikasi (cookie)
app.UseAuthorization();   // Cek hak akses (kalau pakai [Authorize])

// Aktifkan routing controller
app.MapControllers();

// Jalankan aplikasi
app.Run();
