# Backend Development Instructions - Toko Eniwan POS

## ⚠️ CRITICAL CODE GENERATION RULES

### 🚫 **NEVER GENERATE CODE WITHOUT PERMISSION**
- **ALWAYS ASK**: "May I generate the code now, Tuan Maharaja Dika?"
- **EXPLAIN FIRST**: Describe approach, patterns, and reasoning
- **WAIT FOR PERMISSION**: Only proceed after explicit "yes/boleh/silakan"
- **SUGGEST NEXT STEPS**: Provide recommendations after generation

### 🔄 **MANDATORY Workflow:**
```
1. Request: "Create UserController"
2. Response: 
   - "I'll create UserController using Clean Architecture"
   - "It will include CRUD operations with proper authorization"
   - "MAY I GENERATE THE CODE NOW?"
3. Wait for: "yes/boleh/silakan"
4. Generate: Code with proper patterns
5. Suggest: "Next, we should create UserService and DTOs"
```

---

## 🏗️ **Architecture & Stack**

### Technology Stack
- **.NET 9 Web API** with Clean Architecture
- **Entity Framework Core** with SQL Server
- **MediatR** for CQRS pattern
- **FluentValidation** for input validation
- **Cookie-based Authentication** with role-based authorization
- **SignalR** for real-time notifications
- **Background Services** for scheduled tasks

### Project Structure
```
Berca_Backend/
├── Controllers/           # API endpoints with authorization
├── Services/             # Business logic implementation
├── Models/               # Entity models with validation
├── DTOs/                 # Data transfer objects
├── Data/                 # DbContext and configurations
├── BackgroundServices/   # Notification jobs and scheduled tasks
├── Validators/           # FluentValidation classes
└── Extensions/           # Service registration extensions
```

---

## 🎯 **Coding Standards & Patterns**

### Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Always add authorization
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "Inventory.Read")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
        [FromQuery] ProductQueryParams queryParams)
    {
        try
        {
            var result = await _productService.GetProductsAsync(queryParams);
            return Ok(new ApiResponse<PagedResult<ProductDto>>
            {
                Success = true,
                Data = result,
                Message = "Products retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }
}
```

### Service Layer Pattern
```csharp
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParams queryParams);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
    Task<bool> DeleteProductAsync(int id);
    Task<List<ProductDto>> GetExpiringProductsAsync(int daysAhead = 30);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly INotificationService _notificationService;

    public ProductService(
        AppDbContext context, 
        ILogger<ProductService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
    {
        try
        {
            // Validation
            if (await _context.Products.AnyAsync(p => p.Barcode == createProductDto.Barcode))
            {
                throw new ArgumentException("Product with this barcode already exists");
            }

            // Map DTO to entity
            var product = new Product
            {
                Name = createProductDto.Name,
                Barcode = createProductDto.Barcode,
                ExpiryDate = createProductDto.ExpiryDate, // MANDATORY
                BuyPrice = createProductDto.BuyPrice,
                SellPrice = createProductDto.SellPrice,
                Stock = createProductDto.Stock,
                CategoryId = createProductDto.CategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Auto-generate expiry notifications
            await _notificationService.CreateExpiryNotificationsAsync(product);

            return MapToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            throw;
        }
    }
}
```

---

## 🗄️ **Enhanced Database Models**

### Core Models with Mandatory Expiry
```csharp
// Enhanced Product Model - MANDATORY EXPIRY
public class Product
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;
    
    [Required] // MANDATORY EXPIRY DATE
    public DateTime ExpiryDate { get; set; }
    
    [MaxLength(50)]
    public string? BatchNumber { get; set; }
    
    public DateTime? ManufacturedDate { get; set; }
    
    public bool ExpiryNotificationSent { get; set; } = false;
    
    public bool IsPerishable { get; set; } = true;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal BuyPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal SellPrice { get; set; }
    
    public int Stock { get; set; }
    public int MinimumStock { get; set; } = 5;
    
    [MaxLength(20)]
    public string Unit { get; set; } = "pcs";
    
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Computed properties
    [NotMapped]
    public bool IsExpiringSoon => ExpiryDate <= DateTime.Now.AddDays(30);
    
    [NotMapped]
    public bool IsExpired => ExpiryDate <= DateTime.Now;
    
    [NotMapped]
    public decimal ProfitMargin => BuyPrice > 0 ? ((SellPrice - BuyPrice) / BuyPrice) * 100 : 0;
}

// Enhanced User Model with Theme & Notifications
public class User
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required, MaxLength(20)]
    public string Role { get; set; } = "User"; // Admin, Manager, User, Cashier
    
    [MaxLength(10)]
    public string PreferredTheme { get; set; } = "light"; // light, dark
    
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual UserProfile? UserProfile { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
```

### Supplier & Facture Management Models
```csharp
// Supplier Management
public class Supplier
{
    public int Id { get; set; }
    
    [Required, MaxLength(20)]
    public string SupplierCode { get; set; } = string.Empty; // SUP001, SUP002
    
    [Required, MaxLength(100)]
    public string CompanyName { get; set; } = string.Empty; // Indofood, Unilever
    
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    [MaxLength(50)]
    public string? City { get; set; }
    
    [MaxLength(50)]
    public string? Province { get; set; }
    
    [MaxLength(50)]
    public string? TaxId { get; set; } // NPWP
    
    public int PaymentTerms { get; set; } = 30; // days
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public SupplierType SupplierType { get; set; } = SupplierType.Distributor;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Facture> Factures { get; set; } = new List<Facture>();
}

public enum SupplierType
{
    Manufacturer,
    Distributor,
    Wholesaler
}

// Facture Management
public class Facture
{
    public int Id { get; set; }
    
    [Required, MaxLength(30)]
    public string FactureNumber { get; set; } = string.Empty; // FAC-2025-001
    
    [Required]
    public int SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;
    
    [Required]
    public DateTime PurchaseDate { get; set; }
    
    [Required]
    public DateTime DueDate { get; set; }
    
    public DateTime? DeliveryDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0; // PPN 11%
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;
    
    public FactureStatus Status { get; set; } = FactureStatus.Draft;
    
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Transfer;
    
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; } // PO/Invoice number
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public int? ApprovedBy { get; set; }
    public virtual User? ApprovalUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    [Required]
    public int CreatedBy { get; set; }
    public virtual User CreatedUser { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<FactureItem> FactureItems { get; set; } = new List<FactureItem>();
    public virtual ICollection<FacturePayment> Payments { get; set; } = new List<FacturePayment>();
    
    // Computed properties
    [NotMapped]
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    
    [NotMapped]
    public bool IsOverdue => Status != FactureStatus.Paid && DueDate < DateTime.Now;
    
    [NotMapped]
    public bool IsDueSoon => Status != FactureStatus.Paid && DueDate <= DateTime.Now.AddDays(3);
}

public enum FactureStatus
{
    Draft,
    Pending,
    Approved,
    PartialPaid,
    Paid,
    Overdue,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    Transfer,
    Check,
    Giro
}
```

### Enhanced Notification System
```csharp
// Enhanced Notification Model
public class Notification
{
    public int Id { get; set; }
    
    public int? UserId { get; set; } // null = broadcast to all
    public virtual User? User { get; set; }
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public int? RelatedId { get; set; } // ProductId, FactureId, etc.
    
    [MaxLength(50)]
    public string? RelatedType { get; set; } // "Product", "Facture"
    
    [MaxLength(200)]
    public string? ActionUrl { get; set; } // Frontend route
    
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
    
    public bool IsRead { get; set; } = false;
    public bool IsBrowserSent { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    // Stock & Inventory
    LowStock,
    StockOut,
    InventoryAudit,
    
    // Product Expiry (Enhanced)
    ProductExpiry3Months,
    ProductExpiry1Month,
    ProductExpiry2Weeks,
    ProductExpiryToday,
    ProductExpired,
    
    // Facture & Payments
    FactureDue3Days,
    FactureDue2Days,
    FactureDue1Day,
    FactureOverdue,
    FactureApproved,
    PaymentReceived,
    
    // Sales & Revenue
    DailySalesTarget,
    MonthlySalesTarget,
    HighValueTransaction,
    
    // System
    SystemMaintenance,
    BackupCompleted,
    UserLogin,
    SecurityAlert
}

public enum NotificationPriority
{
    Low,      // Background info, no immediate action
    Medium,   // Standard notifications
    High,     // Requires attention soon
    Critical  // Immediate action required
}

// Push Notification Subscription
public class NotificationSubscription
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    [Required, MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;
    
    [Required, MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;
    
    [Required, MaxLength(200)]
    public string Auth { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? UserAgent { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## 🔐 **Authentication & Authorization**

### Cookie Authentication Setup
```csharp
// Program.cs - Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TokoEniwanAuth";
        options.Cookie.HttpOnly = false; // Allow frontend access
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/api/Auth/login";
        options.LogoutPath = "/api/Auth/logout";
        
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

// Policy-based Authorization
builder.Services.AddAuthorization(options =>
{
    // Inventory permissions
    options.AddPolicy("Inventory.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User", "Cashier"));
    options.AddPolicy("Inventory.Write", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    
    // Facture permissions
    options.AddPolicy("Facture.Read", policy =>
        policy.RequireRole("Admin", "Manager", "User"));
    options.AddPolicy("Facture.Write", policy =>
        policy.RequireRole("Admin", "Manager"));
    
    // Reports permissions
    options.AddPolicy("Reports.Export", policy =>
        policy.RequireRole("Admin", "Manager"));
    
    // User management permissions
    options.AddPolicy("Users.Manage", policy =>
        policy.RequireRole("Admin", "Manager"));
});
```

### Authorization Usage Examples
```csharp
// Controller-level authorization
[Authorize(Policy = "Inventory.Write")]
public class ProductController : ControllerBase
{
    // All actions require Inventory.Write policy
}

// Action-level authorization
[HttpGet("reports")]
[Authorize(Policy = "Reports.Export")]
public async Task<ActionResult<ReportDto>> ExportReport()
{
    // Only Admin and Manager can export reports
}

// Multiple policies
[HttpPost("approve")]
[Authorize(Policy = "Facture.Write")]
public async Task<ActionResult> ApproveFacture(int factureId)
{
    // Only Admin and Manager can approve factures
}
```

---

## 🔔 **Enhanced Notification System**

### Background Notification Service
```csharp
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    
    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                // Daily checks at 6 AM
                if (DateTime.Now.Hour == 6 && DateTime.Now.Minute < 5)
                {
                    await notificationService.CheckProductExpiryNotificationsAsync();
                    await notificationService.CheckFactureDueNotificationsAsync();
                }
                
                // Hourly low stock checks
                if (DateTime.Now.Minute < 5)
                {
                    await notificationService.CheckLowStockNotificationsAsync();
                }
                
                // Wait 5 minutes before next check
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification background service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

// Notification Service Interface
public interface INotificationService
{
    Task<NotificationSummaryDto> GetNotificationSummaryAsync(int? userId = null);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task CreateNotificationAsync(CreateNotificationDto notification);
    Task CheckProductExpiryNotificationsAsync();
    Task CheckFactureDueNotificationsAsync();
    Task CheckLowStockNotificationsAsync();
    Task<bool> SubscribeToPushNotificationsAsync(int userId, PushSubscriptionDto subscription);
    Task SendPushNotificationAsync(int userId, string title, string message, NotificationPriority priority);
}
```

### Push Notification Service
```csharp
public interface IPushNotificationService
{
    Task<bool> SendNotificationAsync(NotificationSubscription subscription, object payload);
    Task<bool> SendToUserAsync(int userId, string title, string message, NotificationPriority priority);
    Task<bool> SendToAllUsersAsync(string title, string message, NotificationPriority priority);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;
    
    public async Task<bool> SendNotificationAsync(NotificationSubscription subscription, object payload)
    {
        try
        {
            var vapidKeys = new VapidDetails(
                subject: _configuration["Vapid:Subject"],
                publicKey: _configuration["Vapid:PublicKey"],
                privateKey: _configuration["Vapid:PrivateKey"]
            );
            
            var webPushClient = new WebPushClient();
            var payloadJson = JsonSerializer.Serialize(payload);
            
            await webPushClient.SendNotificationAsync(
                new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                payloadJson,
                vapidKeys
            );
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to subscription {SubscriptionId}", subscription.Id);
            return false;
        }
    }
}
```

---

## 🎯 **API Response Standards**

### Standard Response Format
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

// Error Response Format
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public string? StackTrace { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

## 🔄 **CRUD Operation Templates**

### Query Parameters Pattern
```csharp
public class ProductQueryParams
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsExpiringSoon { get; set; }
    public bool? IsLowStock { get; set; }
    public DateTime? ExpiryDateFrom { get; set; }
    public DateTime? ExpiryDateTo { get; set; }
    public string SortBy { get; set; } = "Name";
    public string SortDirection { get; set; } = "ASC"; // ASC, DESC
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FactureQueryParams
{
    public int? SupplierId { get; set; }
    public FactureStatus? Status { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsOverdue { get; set; }
    public bool? IsDueSoon { get; set; }
    public string SortBy { get; set; } = "DueDate";
    public string SortDirection { get; set; } = "ASC";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

---

## 🔧 **Validation & Error Handling**

### FluentValidation Examples
```csharp
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters");
            
        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Barcode is required")
            .MaximumLength(50).WithMessage("Barcode cannot exceed 50 characters")
            .Matches(@"^[0-9]+$").WithMessage("Barcode must contain only numbers");
            
        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("Expiry date is required")
            .GreaterThan(DateTime.Today).WithMessage("Expiry date must be in the future");
            
        RuleFor(x => x.BuyPrice)
            .GreaterThan(0).WithMessage("Buy price must be greater than 0");
            
        RuleFor(x => x.SellPrice)
            .GreaterThan(x => x.BuyPrice).WithMessage("Sell price must be greater than buy price");
            
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");
    }
}

public class CreateFactureDtoValidator : AbstractValidator<CreateFactureDto>
{
    public CreateFactureDtoValidator()
    {
        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("Supplier is required");
            
        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Purchase date is required")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Purchase date cannot be in the future");
            
        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required")
            .GreaterThanOrEqualTo(x => x.PurchaseDate).WithMessage("Due date must be after purchase date");
            
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required")
            .Must(items => items.All(item => item.ExpiryDate.HasValue))
            .WithMessage("All items must have expiry dates"); // MANDATORY EXPIRY
    }
}
```

### Global Exception Handler
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred while processing your request"
        };

        switch (exception)
        {
            case ArgumentException argEx:
                context.Response.StatusCode = 400;
                response.Message = argEx.Message;
                break;
            case UnauthorizedAccessException:
                context.Response.StatusCode = 401;
                response.Message = "Unauthorized access";
                break;
            case KeyNotFoundException:
                context.Response.StatusCode = 404;
                response.Message = "Resource not found";
                break;
            case ValidationException validationEx:
                context.Response.StatusCode = 400;
                response.Message = "Validation failed";
                response.Errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
                break;
            default:
                context.Response.StatusCode = 500;
                break;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

---

## ⚡ **Performance Guidelines**

### EF Core Best Practices
```csharp
// Use AsNoTracking for read-only queries
public async Task<List<ProductDto>> GetProductsForDisplayAsync()
{
    return await _context.Products
        .AsNoTracking()
        .Where(p => p.IsActive)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Barcode = p.Barcode,
            ExpiryDate = p.ExpiryDate,
            Stock = p.Stock,
            CategoryName = p.Category.Name
        })
        .ToListAsync();
}

// Use Select projections to avoid loading unnecessary data
public async Task<PagedResult<ProductSummaryDto>> GetProductSummariesAsync(ProductQueryParams queryParams)
{
    var query = _context.Products
        .AsNoTracking()
        .Where(p => p.IsActive);
    
    // Apply filters
    if (!string.IsNullOrEmpty(queryParams.SearchTerm))
    {
        query = query.Where(p => p.Name.Contains(queryParams.SearchTerm) || 
                                p.Barcode.Contains(queryParams.SearchTerm));
    }
    
    if (queryParams.CategoryId.HasValue)
    {
        query = query.Where(p => p.CategoryId == queryParams.CategoryId.Value);
    }
    
    if (queryParams.IsExpiringSoon.HasValue && queryParams.IsExpiringSoon.Value)
    {
        query = query.Where(p => p.ExpiryDate <= DateTime.Now.AddDays(30));
    }
    
    // Get total count for pagination
    var totalCount = await query.CountAsync();
    
    // Apply sorting and pagination
    var items = await query
        .OrderBy(p => p.Name) // Default sorting
        .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
        .Take(queryParams.PageSize)
        .Select(p => new ProductSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Stock = p.Stock,
            ExpiryDate = p.ExpiryDate,
            IsExpiringSoon = p.ExpiryDate <= DateTime.Now.AddDays(30)
        })
        .ToListAsync();
    
    return new PagedResult<ProductSummaryDto>
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = queryParams.PageNumber,
        PageSize = queryParams.PageSize
    };
}

// Use Include wisely and only when necessary
public async Task<FactureDetailDto?> GetFactureWithDetailsAsync(int factureId)
{
    return await _context.Factures
        .Include(f => f.Supplier)
        .Include(f => f.FactureItems)
            .ThenInclude(fi => fi.Product)
        .Include(f => f.Payments)
        .Where(f => f.Id == factureId)
        .Select(f => new FactureDetailDto
        {
            Id = f.Id,
            FactureNumber = f.FactureNumber,
            SupplierName = f.Supplier.CompanyName,
            TotalAmount = f.TotalAmount,
            PaidAmount = f.PaidAmount,
            Status = f.Status.ToString(),
            Items = f.FactureItems.Select(fi => new FactureItemDto
            {
                ProductName = fi.Product.Name,
                Quantity = fi.OrderedQuantity,
                UnitCost = fi.UnitCost,
                ExpiryDate = fi.ExpiryDate
            }).ToList()
        })
        .FirstOrDefaultAsync();
}
```

---

## 📝 **Code Generation Guidelines**

### Template Patterns for Copilot

#### 1. Controller Template
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class [EntityName]Controller : ControllerBase
{
    private readonly I[EntityName]Service _service;
    private readonly ILogger<[EntityName]Controller> _logger;

    public [EntityName]Controller(I[EntityName]Service service, ILogger<[EntityName]Controller> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "[Entity].Read")]
    public async Task<ActionResult<ApiResponse<PagedResult<[EntityName]Dto>>>> Get[EntityName]s(
        [FromQuery] [EntityName]QueryParams queryParams)
    {
        // Implementation with try-catch and proper logging
    }

    [HttpPost]
    [Authorize(Policy = "[Entity].Write")]
    public async Task<ActionResult<ApiResponse<[EntityName]Dto>>> Create[EntityName](
        Create[EntityName]Dto create[EntityName]Dto)
    {
        // Implementation with validation and error handling
    }
}
```

#### 2. Service Template
```csharp
public interface I[EntityName]Service
{
    Task<PagedResult<[EntityName]Dto>> Get[EntityName]sAsync([EntityName]QueryParams queryParams);
    Task<[EntityName]Dto?> Get[EntityName]ByIdAsync(int id);
    Task<[EntityName]Dto> Create[EntityName]Async(Create[EntityName]Dto create[EntityName]Dto);
    Task<[EntityName]Dto> Update[EntityName]Async(int id, Update[EntityName]Dto update[EntityName]Dto);
    Task<bool> Delete[EntityName]Async(int id);
}

public class [EntityName]Service : I[EntityName]Service
{
    private readonly AppDbContext _context;
    private readonly ILogger<[EntityName]Service> _logger;
    private readonly IMapper _mapper;

    public [EntityName]Service(AppDbContext context, ILogger<[EntityName]Service> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    // Implement interface methods with proper error handling and logging
}
```

#### 3. Validator Template
```csharp
public class Create[EntityName]DtoValidator : AbstractValidator<Create[EntityName]Dto>
{
    public Create[EntityName]DtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
        
        // Add more validation rules based on entity requirements
    }
}
```

---

## 🚀 **Development Workflow**

### Before Generating Any Code:

1. **Understand Requirements**
   - What entity/feature needs to be implemented?
   - What business rules apply?
   - What authorization policies are needed?

2. **Explain Approach**
   - Which patterns will be used (Controller → Service → Repository)?
   - What validation rules are needed?
   - How will errors be handled?

3. **Ask Permission**
   - "May I generate the code now, Tuan Maharaja Dika?"
   - Wait for explicit permission: "yes/boleh/silakan"

4. **Generate Code**
   - Follow the established patterns
   - Include proper error handling
   - Add comprehensive logging
   - Include validation and authorization

5. **Suggest Next Steps**
   - What should be implemented next?
   - Any dependencies that need to be created?
   - Testing recommendations

### Example Workflow:
```
Request: "Create ProductController for inventory management"

Response:
"I'll create ProductController with the following approach:
- Use Clean Architecture pattern with dependency injection
- Include CRUD operations with proper authorization policies
- Add comprehensive error handling and logging
- Implement pagination and filtering capabilities
- Include validation for mandatory expiry dates
- Follow the established API response format

MAY I GENERATE THE CODE NOW, Tuan Maharaja Dika?"

Wait for permission, then generate code and suggest next steps.
```

---

## 📋 **Essential Checklist for All Backend Code**

### ✅ **Must Have:**
- [ ] Proper authorization attributes (`[Authorize(Policy = "...")]`)
- [ ] Comprehensive error handling with try-catch blocks
- [ ] Logging for all operations (Info, Warning, Error)
- [ ] Input validation with FluentValidation
- [ ] Standard ApiResponse<T> format for all endpoints
- [ ] Async/await for all database operations
- [ ] Proper HTTP status codes (200, 201, 400, 401, 403, 404, 500)
- [ ] XML documentation comments for public methods
- [ ] MANDATORY expiry date validation for product-related operations

### ✅ **Performance:**
- [ ] Use AsNoTracking() for read-only queries
- [ ] Use Select projections to limit data transfer
- [ ] Implement proper pagination
- [ ] Include efficient filtering and sorting
- [ ] Avoid N+1 query problems

### ✅ **Security:**
- [ ] Role-based authorization on all endpoints
- [ ] Input sanitization and validation
- [ ] Prevent SQL injection (use parameterized queries)
- [ ] Secure password hashing
- [ ] Proper CORS configuration

---

> **Remember**: NEVER generate code without explicit permission from Tuan Maharaja Dika. Always explain approach first, ask permission, then generate clean, secure, and performant code following these patterns.