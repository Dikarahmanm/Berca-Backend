using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Berca_Backend.Data;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ Add Entity Framework with enhanced configuration
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enhanced SQL Server options for reliability
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
            
        // Set command timeout for long-running queries
        sqlOptions.CommandTimeout(30);
    })
    // Query splitting is configured at the SQL Server level
    .EnableServiceProviderCaching() // Improve performance
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()) // Development logging
    .ConfigureWarnings(warnings =>
    {
        // Handle multiple collection include warning based on environment
        if (builder.Environment.IsDevelopment())
        {
            warnings.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
        }
        else
        {
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
        }
    });
});



// ✅ Add controllers
builder.Services.AddControllers();

// ✅ CORS Configuration - Enhanced for Development
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
              .AllowCredentials() // 🔧 Essential for cookies to work cross-origin
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

// ✅ Authentication - Enhanced for Development Cross-Origin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "TokoEniwanAuth";
        options.Cookie.HttpOnly = false; // 🔧 Allow frontend access in development
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.None  // 🔧 Allow HTTP cookies in development
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax; // 🔧 Essential for cross-port requests
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

    // ===== MEMBER CREDIT SYSTEM POLICIES ===== //

    // Credit granting operations - High-level approval required
    options.AddPolicy("Membership.GrantCredit", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Payment recording operations - Operational staff can record payments
    options.AddPolicy("Membership.RecordPayment", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Credit status updates - Management approval required
    options.AddPolicy("Membership.UpdateCredit", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Collections management - Staff can assist with collections
    options.AddPolicy("Membership.Collections", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Credit analytics and reporting - Management and staff can view
    options.AddPolicy("Membership.Analytics", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Credit eligibility checking - Operational staff can check eligibility
    options.AddPolicy("Membership.CheckEligibility", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Payment reminders - Management can send reminders
    options.AddPolicy("Membership.SendReminders", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Credit history access - All authorized users can view history
    options.AddPolicy("Membership.CreditHistory", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Bulk credit operations - Admin/HeadManager only for safety
    options.AddPolicy("Membership.BulkCredit", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    // ===== EXPIRY MANAGEMENT SYSTEM POLICIES ===== //

    // Expiry data access - All operational staff can view expiry data
    options.AddPolicy("Expiry.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Batch management - Operational staff can manage batches
    options.AddPolicy("Expiry.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Disposal operations - Management approval required for disposal
    options.AddPolicy("Expiry.Dispose", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // FIFO operations - Operational staff can process FIFO sales
    options.AddPolicy("Expiry.FIFO", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // ===== AI INVENTORY COORDINATION POLICIES ===== //

    // AI system administration - Admin only for system management
    options.AddPolicy("Admin.AI", policy =>
        policy.RequireRole("Admin"));

    // Cache system administration - Admin only for cache management
    options.AddPolicy("Admin.Cache", policy =>
        policy.RequireRole("Admin"));

    // AI model training - HeadManager and Admin can train models
    options.AddPolicy("AI.ModelTraining", policy =>
        policy.RequireRole("Admin", "HeadManager"));

    // Auto-optimization execution - Management approval required
    options.AddPolicy("MultiBranch.AutoOptimize", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Advanced notification system - Management can configure notifications
    options.AddPolicy("Notifications.Advanced", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // ===== MULTI-BRANCH NOTIFICATION POLICIES ===== //
    
    // Create notifications - Management and staff can create notifications
    options.AddPolicy("Notification.Create", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));
        
    // Delete notifications - Only creators, managers and admins can delete
    options.AddPolicy("Notification.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // AI analytics and reporting - Management and staff can view
    options.AddPolicy("Reports.Analytics", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // ===== ANALYTICS CONTROLLER POLICIES ===== //

    // Analytics dashboard data access - Management and staff can view
    options.AddPolicy("Analytics.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Analytics data modification - Management can modify analytics settings
    options.AddPolicy("Analytics.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // System monitoring - All authenticated users can view system status
    options.AddPolicy("Reports.System", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Expiry analytics - Management can view analytics
    options.AddPolicy("Expiry.Analytics", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Expiry notifications - Staff can manage notifications
    options.AddPolicy("Expiry.Notifications", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Daily expiry checks - Automated background service access
    options.AddPolicy("Expiry.BackgroundTasks", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

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

    // ===== FACTURE MANAGEMENT POLICIES ===== //

    // Facture receiving operations
    options.AddPolicy("Facture.Receive", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Facture verification workflow
    options.AddPolicy("Facture.Verify", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Facture approval workflow (high-value invoices)
    options.AddPolicy("Facture.Approve", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Facture dispute management
    options.AddPolicy("Facture.Dispute", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Payment scheduling and processing
    options.AddPolicy("Facture.SchedulePayment", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("Facture.ProcessPayment", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Payment confirmation from suppliers
    options.AddPolicy("Facture.ConfirmPayment", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Facture update operations
    options.AddPolicy("Facture.Update", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Facture cancellation operations
    options.AddPolicy("Facture.Cancel", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Payment update operations
    options.AddPolicy("Facture.UpdatePayment", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Payment cancellation operations  
    options.AddPolicy("Facture.CancelPayment", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Facture read access
    options.AddPolicy("Facture.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Facture analytics and reporting
    options.AddPolicy("Facture.Analytics", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Facture bulk operations
    options.AddPolicy("Facture.BulkOperations", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Facture validation and compliance
    options.AddPolicy("Facture.Validate", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

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

    // ===== SPRINT 8: ADVANCED REPORTING & ANALYTICS POLICIES ===== //

    // Report execution and generation
    options.AddPolicy("Reports.Execute", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Report export (PDF, Excel, CSV)
    options.AddPolicy("Reports.Export", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Business Intelligence analytics
    options.AddPolicy("Reports.Analytics", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Advanced reporting features (scheduling, templates)
    options.AddPolicy("Reports.Advanced", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Report management (create, edit, delete custom reports)
    options.AddPolicy("Reports.Manage", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Sales reporting
    options.AddPolicy("Reports.Sales", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Inventory reporting
    options.AddPolicy("Reports.Inventory", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Financial reporting (sensitive data)
    options.AddPolicy("Reports.Financial", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Credit reporting (sensitive member credit data)
    options.AddPolicy("Reports.Credit", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Supplier performance reporting
    options.AddPolicy("Reports.Supplier", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Business Intelligence dashboards
    options.AddPolicy("Reports.Dashboard", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Predictive analytics and forecasting
    options.AddPolicy("Reports.Forecasting", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Report scheduling and automation
    options.AddPolicy("Reports.Schedule", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager"));

    // Report email and distribution
    options.AddPolicy("Reports.Distribution", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Notifications Policies
    options.AddPolicy("Notifications.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Notifications.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // Calendar & Events Policies
    options.AddPolicy("Calendar.Read", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Calendar.Write", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("Calendar.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("Calendar.Manage", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    // User Management - Additional Policies
    options.AddPolicy("User.Create", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("User.Update", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    options.AddPolicy("User.Delete", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("User.View", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // Manager Operations
    options.AddPolicy("Manager.Manage", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager"));

    options.AddPolicy("Manager.View", policy =>
        policy.RequireRole("Admin", "HeadManager", "BranchManager", "Manager", "User"));

    // MultiBranch Operations
    options.AddPolicy("MultiBranch.Access", policy =>
        policy.RequireRole("Admin", "HeadManager"));

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

    // ===== MEMBER CREDIT - POS INTEGRATION POLICIES ===== //

    // POS credit transaction policies
    options.AddPolicy("POS.CreditTransaction", policy => 
        policy.RequireRole("Cashier", "Manager", "BranchManager", "HeadManager", "Admin"));
        
    options.AddPolicy("POS.CreditValidation", policy => 
        policy.RequireRole("Cashier", "Manager", "BranchManager", "HeadManager", "Admin"));

    // Member credit information access for different modules
    options.AddPolicy("Member.CreditInfo", policy => 
        policy.RequireRole("User", "Manager", "BranchManager", "HeadManager", "Admin"));

    // High-value credit transaction approval
    options.AddPolicy("POS.CreditApproval", policy => 
        policy.RequireRole("Manager", "BranchManager", "HeadManager", "Admin"));

    // Credit payment processing
    options.AddPolicy("POS.CreditPayment", policy => 
        policy.RequireRole("Cashier", "Manager", "BranchManager", "HeadManager", "Admin"));
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

// ✅ Register Cache Invalidation Service
builder.Services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();

// ✅ Register Cache Warmup Service
builder.Services.AddScoped<ICacheWarmupService, CacheWarmupService>();

// ✅ Register Cache Warmup Hosted Service for startup warmup
builder.Services.AddHostedService<CacheWarmupHostedService>();

// ✅ Register Cache Refresh Background Service for periodic refresh
builder.Services.AddHostedService<CacheRefreshBackgroundService>();

// ✅ Register services in correct dependency order
builder.Services.AddScoped<ITimezoneService, TimezoneService>(); // FIRST - other services depend on this
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
// ✅ Basic notification service removed - using unified MultiBranch system instead
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IPOSService, POSService>();
builder.Services.AddScoped<IUserBranchAssignmentService, UserBranchAssignmentService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IConsolidatedReportService, ConsolidatedReportService>();
builder.Services.AddScoped<IInventoryTransferService, InventoryTransferService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IFactureService, FactureService>();

// ✅ Add ExpiryManagement service
builder.Services.AddScoped<Berca_Backend.Services.Interfaces.IExpiryManagementService, Berca_Backend.Services.ExpiryManagementService>();

// ✅ Add Push Notification services
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddHostedService<BrowserNotificationService>();

// ✅ Add Calendar & Events services
builder.Services.AddScoped<ICalendarEventService, CalendarEventService>();
builder.Services.AddHostedService<EventReminderService>();

// ✅ Add Member Credit Background Service
builder.Services.AddMemberCreditBackgroundService();

// ✅ Add Facture Background Service
builder.Services.AddFactureBackgroundService();

// ✅ Add Batch Expiry Monitoring Background Service
builder.Services.AddHostedService<Berca_Backend.Services.Background.BatchExpiryMonitoringService>();
builder.Services.AddHostedService<AIInventoryBackgroundService>();

// ✅ Add ML Model Training Background Service
builder.Services.AddHostedService<MLModelTrainingBackgroundService>();

// ✅ Add Sprint 8: Advanced Reporting & Analytics Services
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExportService, ExportService>();

// ✅ Add Business Rules Service for centralized business logic
builder.Services.AddScoped<IBusinessRulesService, BusinessRulesService>();

// ✅ Add Advanced Analytics Services (Final Backend Implementation)
builder.Services.AddScoped<IMultiBranchCoordinationService, MultiBranchCoordinationService>();
builder.Services.AddScoped<IAIInventoryCoordinationService, AIInventoryCoordinationService>();
builder.Services.AddScoped<IMLInventoryService, MLInventoryService>();

// ✅ Multi-Branch Notification System
builder.Services.AddScoped<IMultiBranchNotificationService, MultiBranchNotificationService>();

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

// ✅ Add logging with reduced noise for development debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure logging levels for better debugging experience
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Transaction", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Warning);
    
    // Background services - reduce noise
    builder.Logging.AddFilter("Berca_Backend.Services.PushNotificationService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.BrowserNotificationService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.EventReminderService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.MemberCreditBackgroundService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.FactureBackgroundService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.Background.BatchExpiryMonitoringService", LogLevel.Warning);
    builder.Logging.AddFilter("Berca_Backend.Services.AIInventoryBackgroundService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.AIInventoryCoordinationService", LogLevel.Error);
    builder.Logging.AddFilter("Berca_Backend.Services.TimezoneService", LogLevel.Error);
    
    // Keep important controllers and startup visible
    builder.Logging.AddFilter("Berca_Backend.Controllers", LogLevel.Information);
    builder.Logging.AddFilter("Program", LogLevel.Information);
}

// ✅ Add SignalR for real-time coordination
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// ✅ Add cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

var app = builder.Build();

// Check for command line arguments for database operations
if (args.Length > 0 && args[0].ToLower() == "--reset-database")
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await DatabaseResetUtility.ResetDatabaseAsync(context, logger, forceReset: true);
        
        logger.LogInformation("🎉 Database reset completed successfully! Exiting...");
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database reset failed: {ex.Message}");
        return;
    }
}

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

// Create facture upload directories
var facturesPath = Path.Combine(uploadsPath, "factures");
Directory.CreateDirectory(facturesPath);
Directory.CreateDirectory(Path.Combine(facturesPath, "invoices"));
Directory.CreateDirectory(Path.Combine(facturesPath, "receipts"));
Directory.CreateDirectory(Path.Combine(facturesPath, "supporting"));

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

// 🔧 DEVELOPMENT BYPASS: Add mock authentication for development
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        // Only apply to API endpoints that need authentication
        if (context.Request.Path.StartsWithSegments("/api") && 
            !context.User.Identity?.IsAuthenticated == true)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("🔧 DEV BYPASS: Creating mock authenticated user for {Path}", context.Request.Path);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "dev-user"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("UserId", "1"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("Role", "Admin"),
                new Claim("BranchId", "1")
            };
            
            var identity = new ClaimsIdentity(claims, "dev-bypass");
            context.User = new ClaimsPrincipal(identity);
        }
        
        await next();
    });
}

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

// ✅ Configure SignalR hubs
app.MapHub<Berca_Backend.Hubs.AIInventoryCoordinationHub>("/hubs/ai-inventory-coordination");
app.MapHub<Berca_Backend.Services.MultiBranchNotificationHub>("/multibranch-notification-hub");

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
startupLogger.LogInformation("📁 Directories Created: wwwroot, uploads, exports, factures");
startupLogger.LogInformation("📄 Facture Management System: ENABLED");
startupLogger.LogInformation("🤖 ML Model Training Service: ENABLED (24h training, 6h health checks)");

// ✅ IMPROVED: Enhanced database setup with proper error handling
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("🔄 Starting database setup...");
        
        // ✅ FIX: Set connection timeout to prevent deadlocks
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        
        // ✅ FIX: Use safer database setup approach
        await SafeDatabaseSetupAsync(context, logger);
        
        logger.LogInformation("📊 Database setup completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database setup or seeding");
        // Don't crash the application, just log the error
        logger.LogWarning("⚠️ Application will continue without sample data");
    }
}

// ✅ Run the application
await app.RunAsync();

// ==================== SAFE DATABASE SETUP METHODS ==================== //

/// <summary>
/// Safe database setup method with concurrency fixes
/// </summary>
static async Task SafeDatabaseSetupAsync(AppDbContext context, ILogger logger)
{
    try
    {
        // Check database connectivity first
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogInformation("📦 Creating new database...");
            await context.Database.EnsureCreatedAsync();
        }

        // Apply pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"🔄 Applying {pendingMigrations.Count()} pending migrations...");
            await context.Database.MigrateAsync();
        }

        // Seed data with retry mechanism
        await SeedDataWithRetryAsync(context, logger);
        
        // Simple integrity check (no complex operations)
        await SimpleIntegrityCheckAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Safe database setup failed");
        throw;
    }
}

/// <summary>
/// Retry mechanism for seeding with progressive delay
/// </summary>
static async Task SeedDataWithRetryAsync(AppDbContext context, ILogger logger, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await SampleDataSeeder.SeedSampleDataAsync(context, logger);
            return; // Success
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, $"⚠️ Seeding attempt {attempt} failed, retrying...");
            await Task.Delay(1000 * attempt); // Progressive delay
        }
    }
    
    // Final attempt without catch
    await SampleDataSeeder.SeedSampleDataAsync(context, logger);
}

/// <summary>
/// Simple integrity check without concurrency issues
/// </summary>
static async Task SimpleIntegrityCheckAsync(AppDbContext context, ILogger logger)
{
    try
    {
        var userCount = await context.Users.CountAsync();
        var branchCount = await context.Branches.CountAsync();
        
        logger.LogInformation($"📊 Basic integrity check: {userCount} users, {branchCount} branches");
        
        if (userCount == 0 || branchCount == 0)
        {
            logger.LogWarning("⚠️ Some tables appear empty, but application will continue");
        }
        else
        {
            logger.LogInformation("✅ Basic integrity check passed");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ Integrity check failed, but application will continue");
    }
}
public partial class Program { }
