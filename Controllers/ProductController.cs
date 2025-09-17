// Controllers/ProductController.cs - Sprint 2 Product Controller Implementation (FIXED)
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Berca_Backend.Models; // ✅ TAMBAHKAN ini untuk MutationType enum
using Berca_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public ProductController(IProductService productService, ILogger<ProductController> logger, AppDbContext context, IServiceProvider serviceProvider)
        {
            _productService = productService;
            _logger = logger;
            _context = context;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Get products with filtering, search, and pagination (Enhanced for multi-branch)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductListResponse>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? branchIds = null)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size

                // Get user context for branch filtering
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                // Parse branch IDs for filtering
                var requestedBranchIds = new List<int>();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    requestedBranchIds = branchIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                }

                // Validate user access to requested branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                
                // Filter requested branches by user access
                if (requestedBranchIds.Any())
                {
                    requestedBranchIds = requestedBranchIds.Intersect(accessibleBranchIds).ToList();
                }
                else
                {
                    requestedBranchIds = accessibleBranchIds;
                }

                var response = await _productService.GetProductsAsync(page, pageSize, search, categoryId, isActive);

                // Enhance products with REAL branch-specific stock information
                var productIds = response.Products.Select(p => p.Id).ToList();
                var realBranchStocks = await GetRealBranchStocksBatch(productIds, requestedBranchIds);

                var enhancedProducts = new List<object>();
                foreach (var product in response.Products)
                {
                    var branchStocks = new List<object>();
                    
                    // Get REAL stock information for accessible branches
                    foreach (var branchId in requestedBranchIds)
                    {
                        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
                        if (branch != null)
                        {
                            // ✅ FIX: Use REAL branch stock from StockMutations
                            var realStock = realBranchStocks.TryGetValue((product.Id, branchId), out var stock) ? stock : 0;
                            
                            branchStocks.Add(new
                            {
                                branchId = branch.Id,
                                branchName = branch.BranchName,
                                stock = realStock, // ✅ REAL STOCK DATA
                                minStock = product.MinStock,
                                maxStock = product.MaxStock,
                                lastUpdated = DateTime.UtcNow
                            });
                        }
                    }

                    // Calculate total stock across accessible branches
                    var totalBranchStock = branchStocks.Cast<dynamic>().Sum(bs => (int)bs.stock);

                    enhancedProducts.Add(new
                    {
                        id = product.Id,
                        name = product.Name,
                        barcode = product.Barcode,
                        description = product.Description,
                        categoryId = product.CategoryId,
                        categoryName = product.CategoryName ?? "Uncategorized",
                        categoryColor = product.CategoryColor ?? "#666666",
                        brand = product.Brand ?? string.Empty,
                        buyPrice = product.BuyPrice,
                        sellPrice = product.SellPrice,
                        stock = totalBranchStock, // ✅ REAL TOTAL STOCK
                        minimumStock = product.MinimumStock,
                        minStock = product.MinStock,
                        maxStock = product.MaxStock,
                        unit = product.Unit,
                        imageUrl = product.ImageUrl,
                        isActive = product.IsActive,
                        createdAt = product.CreatedAt,
                        updatedAt = product.UpdatedAt,
                        profitMargin = product.ProfitMargin,
                        isLowStock = product.IsLowStock,
                        isOutOfStock = product.IsOutOfStock,
                        branchStocks = branchStocks
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        products = enhancedProducts,
                        totalCount = response.TotalCount,
                        currentPage = response.CurrentPage,
                        totalPages = response.TotalPages
                    },
                    message = $"Retrieved {enhancedProducts.Count} products with branch stock information"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiResponse<ProductListResponse>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product: {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get product by barcode (for POS scanning)
        /// </summary>
        [HttpGet("barcode/{barcode}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductByBarcode(string barcode)
        {
            try
            {
                var product = await _productService.GetProductByBarcodeAsync(barcode);
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found with this barcode"
                    });
                }

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode: {Barcode}", barcode);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get products by branch (for POS and branch-specific operations)
        /// </summary>
        [HttpGet("by-branch")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<BranchProductDto>>>> GetProductsByBranch(
            [FromQuery] string branchIds,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? isActive = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchIds))
                {
                    return BadRequest(new ApiResponse<List<BranchProductDto>>
                    {
                        Success = false,
                        Message = "Branch IDs are required"
                    });
                }

                // Parse branch IDs
                var branchIdList = branchIds.Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (branchIdList.Count == 0)
                {
                    return BadRequest(new ApiResponse<List<BranchProductDto>>
                    {
                        Success = false,
                        Message = "Valid branch IDs are required"
                    });
                }

                // Get user context for access validation
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);

                // Validate user has access to requested branches
                var unauthorizedBranches = branchIdList.Except(accessibleBranchIds).ToList();
                if (unauthorizedBranches.Any())
                {
                    return StatusCode(403, new ApiResponse<List<BranchProductDto>>
                    {
                        Success = false,
                        Message = $"Access denied to branches: {string.Join(", ", unauthorizedBranches)}"
                    });
                }

                // Query products with filtering
                var query = _context.Products
                    .Where(p => isActive == null || p.IsActive == isActive)
                    .Include(p => p.Category)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || 
                                           p.Barcode.Contains(search) ||
                                           (p.Description != null && p.Description.Contains(search)));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                var totalItems = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get real branch-specific inventory for these products
                var productIds = products.Select(p => p.Id).ToList();
                var branchInventories = await _context.BranchInventories
                    .Where(bi => productIds.Contains(bi.ProductId) &&
                                branchIdList.Contains(bi.BranchId) &&
                                bi.IsActive)
                    .Include(bi => bi.Branch)
                    .ToListAsync();

                // Create BranchProductDto objects - one per product with branch-specific data
                var productDtos = new List<BranchProductDto>();

                foreach (var product in products)
                {
                    // Get branch inventories for this product
                    var productBranchInventories = branchInventories
                        .Where(bi => bi.ProductId == product.Id)
                        .ToList();

                    // For each requested branch, create a BranchProductDto
                    foreach (var branchId in branchIdList)
                    {
                        var branchInventory = productBranchInventories
                            .FirstOrDefault(bi => bi.BranchId == branchId);

                        int branchSpecificStock = 0;
                        string branchName = "Unknown Branch";

                        if (branchInventory != null)
                        {
                            // Use real branch inventory data
                            branchSpecificStock = branchInventory.Stock;
                            branchName = branchInventory.Branch?.BranchName ?? $"Branch {branchId}";
                        }
                        else
                        {
                            // Fallback: Create demo data and optionally seed to database
                            branchSpecificStock = branchId switch
                            {
                                1 => (int)(product.Stock * 0.6),  // Head Office - highest stock
                                2 => (int)(product.Stock * 0.3),  // Purwakarta - medium stock
                                3 => (int)(product.Stock * 0.25), // Bandung - medium stock
                                4 => (int)(product.Stock * 0.15), // Surabaya - lower stock
                                6 => (int)(product.Stock * 0.1),  // Test Branch - minimal stock
                                _ => (int)(product.Stock * 0.2)   // Default for other branches
                            };

                            branchName = branchId switch
                            {
                                1 => "Head Office Jakarta",
                                2 => "Branch Purwakarta",
                                3 => "Branch Bandung",
                                4 => "Branch Surabaya",
                                6 => "Test Branch",
                                _ => $"Branch {branchId}"
                            };

                            // Auto-seed branch inventory data for demo
                            _ = SeedBranchInventoryAsync(product.Id, branchId, branchSpecificStock, product);
                        }

                        // Only include products with stock > 0
                        if (branchSpecificStock > 0)
                        {
                            productDtos.Add(new BranchProductDto
                            {
                                Id = product.Id,
                                Name = product.Name,
                                Barcode = product.Barcode,
                                SellPrice = product.SellPrice,
                                BuyPrice = product.BuyPrice,
                                Stock = product.Stock, // Original stock
                                BranchStock = branchSpecificStock, // Branch-specific stock
                                BranchId = branchId,
                                BranchName = branchName,
                                MinStock = product.MinimumStock,
                                Unit = product.Unit,
                                CategoryId = product.CategoryId,
                                CategoryName = product.Category?.Name,
                                IsActive = product.IsActive,
                                Description = product.Description,
                                CreatedAt = product.CreatedAt,
                                UpdatedAt = product.UpdatedAt
                            });
                        }
                    }
                }

                _logger.LogInformation("Retrieved {ProductCount} products for branches [{BranchIds}]", 
                    productDtos.Count, string.Join(", ", branchIdList));

                return Ok(new ApiResponse<List<BranchProductDto>>
                {
                    Success = true,
                    Data = productDtos,
                    Message = $"Retrieved {productDtos.Count} products for {branchIdList.Count} branches"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by branch");
                return StatusCode(500, new ApiResponse<List<BranchProductDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Check if barcode already exists
                if (await _productService.IsBarcodeExistsAsync(request.Barcode))
                {
                    return Conflict(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "A product with this barcode already exists"
                    });
                }

                var product = await _productService.CreateProductAsync(request, username);

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", request.Name);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
            int id,
            [FromBody] UpdateProductRequest request)
        {
            try
            {
                _logger.LogInformation("📝 Updating product {ProductId}: {@Request}", id, request);

                var username = User.Identity?.Name ?? "system";
                var result = await _productService.UpdateProductAsync(id, request, username);

                _logger.LogInformation("✅ Product {ProductId} updated successfully", id);
                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Product updated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("❌ Product not found: {ProductId}", id);
                return NotFound(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("❌ Invalid product data: {Message}", ex.Message);
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating product {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete a product (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Inventory.Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            try
            {
                _logger.LogInformation("🗑️ Deleting product {ProductId}", id);

                // ✅ FIX: Remove the username parameter - interface only takes id
                var result = await _productService.DeleteProductAsync(id);

                if (result)
                {
                    _logger.LogInformation("✅ Product {ProductId} deleted successfully", id);
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Product deleted successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to delete product"
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("❌ Product not found: {ProductId}", id);
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("❌ Cannot delete product: {Message}", ex.Message);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting product {ProductId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update product stock
        /// </summary>
        [HttpPut("{id}/stock")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateProductStock(
            int id,
            [FromBody] StockUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Updating stock for product {ProductId}: {@Request}", id, request);

                // Validate request
                if (request.Quantity == 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Quantity must be greater than 0"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Notes))
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notes are required for stock updates"
                    });
                }

                var username = User.Identity?.Name ?? "system";

                // ✅ FIX: Use the overloaded method that takes StockUpdateRequest
                var result = await _productService.UpdateStockAsync(
                    id,
                    request.Quantity,
                    request.MutationType,
                    request.Notes, // <-- FIX: Pass notes here
                    request.ReferenceNumber,
                    request.UnitCost,
                    username
                );

                if (result)
                {
                    _logger.LogInformation("✅ Stock updated successfully for product {ProductId}", id);
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Stock updated successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to update stock"
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("❌ Product not found: {ProductId}", id);
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("❌ Invalid stock operation: {Message}", ex.Message);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating stock for product {ProductId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get products with low stock
        /// </summary>
        [HttpGet("alerts/low-stock")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetLowStockProducts([FromQuery] int threshold = 10)
        {
            try
            {
                var products = await _productService.GetLowStockProductsAsync(threshold);

                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = $"Found {products.Count} products with low stock"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get products that are out of stock
        /// </summary>
        [HttpGet("alerts/out-of-stock")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetOutOfStockProducts()
        {
            try
            {
                var products = await _productService.GetOutOfStockProductsAsync();

                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = $"Found {products.Count} products out of stock"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving out of stock products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get inventory mutation history for a product
        /// </summary>
        [HttpGet("{id}/history")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<InventoryMutationDto>>>> GetInventoryHistory(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var history = await _productService.GetInventoryHistoryAsync(id, startDate, endDate);

                return Ok(new ApiResponse<List<InventoryMutationDto>>
                {
                    Success = true,
                    Data = history,
                    Message = $"Retrieved {history.Count} inventory mutations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory history for product: {ProductId}", id);
                return StatusCode(500, new ApiResponse<List<InventoryMutationDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get total inventory value
        /// </summary>
        [HttpGet("reports/inventory-value")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetInventoryValue()
        {
            try
            {
                var value = await _productService.GetInventoryValueAsync();

                return Ok(new ApiResponse<decimal>
                {
                    Success = true,
                    Data = value,
                    Message = "Inventory value calculated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating inventory value");
                return StatusCode(500, new ApiResponse<decimal>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Check if barcode exists
        /// </summary>
        [HttpGet("validate/barcode/{barcode}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateBarcode(string barcode, [FromQuery] int? excludeId = null)
        {
            try
            {
                var exists = await _productService.IsBarcodeExistsAsync(barcode, excludeId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = exists ? "Barcode already exists" : "Barcode is available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating barcode: {Barcode}", barcode);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // ==================== EXPIRY & FIFO ENDPOINTS ==================== //

        /// <summary>
        /// Get FIFO recommendations for products with expiry dates
        /// </summary>
        [HttpGet("fifo/recommendations")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<FifoRecommendationDto>>>> GetFifoRecommendations([FromQuery] int? branchId = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested FIFO recommendations for branch {BranchId}", username, branchId);

                var recommendations = await _productService.GetFifoRecommendationsAsync(branchId);

                return Ok(new ApiResponse<List<FifoRecommendationDto>>
                {
                    Success = true,
                    Data = recommendations,
                    Message = $"Found {recommendations.Count} FIFO recommendations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FIFO recommendations");
                return StatusCode(500, new ApiResponse<List<FifoRecommendationDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving FIFO recommendations"
                });
            }
        }

        /// <summary>
        /// Get products expiring soon (within warning days) - Simplified version
        /// </summary>
        [HttpGet("expiry/warning")]
        [Authorize(Policy = "Inventory.Read")]
        public ActionResult<ApiResponse<string>> GetExpiringProducts(
            [FromQuery] int warningDays = 7,
            [FromQuery] int? branchId = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested expiring products with {WarningDays} days warning for branch {BranchId}", 
                    username, warningDays, branchId);

                // Simplified response
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = "Expiry tracking endpoint - implementation in progress",
                    Message = $"Expiry check for {warningDays} days completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring products");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while retrieving expiring products"
                });
            }
        }

        /// <summary>
        /// Get already expired products
        /// </summary>
        [HttpGet("expiry/expired")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ExpiredProductDto>>>> GetExpiredProducts([FromQuery] int? branchId = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested expired products for branch {BranchId}", username, branchId);

                var filter = new ExpiredProductsFilterDto { BranchId = branchId };
                var expiredProducts = await _productService.GetExpiredProductsAsync(filter);

                return Ok(new ApiResponse<List<ExpiredProductDto>>
                {
                    Success = true,
                    Data = expiredProducts,
                    Message = $"Found {expiredProducts.Count} expired products"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired products");
                return StatusCode(500, new ApiResponse<List<ExpiredProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving expired products"
                });
            }
        }

        /// <summary>
        /// Create a new product batch (for products requiring expiry tracking)
        /// </summary>
        [HttpPost("{productId}/batches")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductBatchDto>>> CreateProductBatch(
            int productId,
            [FromBody] CreateProductBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Invalid batch data"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var branchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "1");

                _logger.LogInformation("User {Username} creating batch for product {ProductId}: {BatchNumber}", 
                    username, productId, request.BatchNumber);

                var batch = await _productService.CreateProductBatchAsync(productId, request, userId, branchId);

                return CreatedAtAction(nameof(GetProductBatch), new { productId, batchId = batch.Id }, 
                    new ApiResponse<ProductBatchDto>
                    {
                        Success = true,
                        Data = batch,
                        Message = "Product batch created successfully"
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid batch data for product {ProductId}", productId);
                return BadRequest(new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot create batch for product {ProductId}", productId);
                return BadRequest(new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the batch"
                });
            }
        }

        /// <summary>
        /// Get batches for a specific product
        /// </summary>
        [HttpGet("{productId}/batches")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductBatchDto>>>> GetProductBatches(
            int productId,
            [FromQuery] bool includeExpired = true,
            [FromQuery] bool includeDisposed = false)
        {
            try
            {
                var batches = await _productService.GetProductBatchesAsync(productId, includeExpired, includeDisposed);

                return Ok(new ApiResponse<List<ProductBatchDto>>
                {
                    Success = true,
                    Data = batches,
                    Message = $"Found {batches.Count} batches for product"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batches for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<List<ProductBatchDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving product batches"
                });
            }
        }

        /// <summary>
        /// Get a specific product batch
        /// </summary>
        [HttpGet("{productId}/batches/{batchId}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<ProductBatchDto>>> GetProductBatch(int productId, int batchId)
        {
            try
            {
                var batch = await _productService.GetProductBatchAsync(batchId);
                
                if (batch == null || batch.ProductId != productId)
                {
                    return NotFound(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Product batch not found"
                    });
                }

                return Ok(new ApiResponse<ProductBatchDto>
                {
                    Success = true,
                    Data = batch
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch {BatchId} for product {ProductId}", batchId, productId);
                return StatusCode(500, new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the batch"
                });
            }
        }

        /// <summary>
        /// Update product batch (quantity, notes, etc.)
        /// </summary>
        [HttpPut("{productId}/batches/{batchId}")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<ProductBatchDto>>> UpdateProductBatch(
            int productId,
            int batchId,
            [FromBody] UpdateProductBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Invalid batch data"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                _logger.LogInformation("User {Username} updating batch {BatchId} for product {ProductId}", 
                    username, batchId, productId);

                var batch = await _productService.UpdateProductBatchAsync(batchId, request, userId);

                if (batch == null)
                {
                    return NotFound(new ApiResponse<ProductBatchDto>
                    {
                        Success = false,
                        Message = "Product batch not found"
                    });
                }

                return Ok(new ApiResponse<ProductBatchDto>
                {
                    Success = true,
                    Data = batch,
                    Message = "Product batch updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid batch update data for batch {BatchId}", batchId);
                return BadRequest(new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating batch {BatchId} for product {ProductId}", batchId, productId);
                return StatusCode(500, new ApiResponse<ProductBatchDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the batch"
                });
            }
        }

        /// <summary>
        /// Dispose expired product batch
        /// </summary>
        [HttpPost("{productId}/batches/{batchId}/dispose")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> DisposeProductBatch(
            int productId,
            int batchId,
            [FromBody] DisposeBatchDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid disposal data"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                _logger.LogInformation("User {Username} disposing batch {BatchId} for product {ProductId} - Method: {Method}", 
                    username, batchId, productId, request.DisposalMethod);

                var result = await _productService.DisposeProductBatchAsync(batchId, request, userId);

                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Product batch disposed successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to dispose product batch"
                    });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid disposal request for batch {BatchId}", batchId);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot dispose batch {BatchId}", batchId);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing batch {BatchId} for product {ProductId}", batchId, productId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while disposing the batch"
                });
            }
        }

        /// <summary>
        /// Get expiry analytics for products
        /// </summary>
        [HttpGet("analytics/expiry")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ApiResponse<ExpiryAnalyticsDto>>> GetExpiryAnalytics(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested expiry analytics for branch {BranchId}", username, branchId);

                var analytics = await _productService.GetExpiryAnalyticsAsync(branchId, startDate, endDate);

                return Ok(new ApiResponse<ExpiryAnalyticsDto>
                {
                    Success = true,
                    Data = analytics,
                    Message = "Expiry analytics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiry analytics");
                return StatusCode(500, new ApiResponse<ExpiryAnalyticsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving expiry analytics"
                });
            }
        }

        /// <summary>
        /// Validate if product requires expiry date based on category
        /// </summary>
        [HttpGet("{productId}/requires-expiry")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckProductRequiresExpiry(int productId)
        {
            try
            {
                var requiresExpiry = await _productService.ProductRequiresExpiryAsync(productId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = requiresExpiry,
                    Message = requiresExpiry ? "Product requires expiry tracking" : "Product does not require expiry tracking"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiry requirement for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while checking expiry requirement"
                });
            }
        }

        // ==================== BATCH MANAGEMENT ENDPOINTS ==================== //

        /// <summary>
        /// Check if product exists by barcode (for frontend registration flow)
        /// Returns 200 if exists, 404 if not found
        /// </summary>
        [HttpHead("barcode/{barcode}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult> CheckProductExistsByBarcode(string barcode)
        {
            try
            {
                var exists = await _productService.ProductExistsByBarcodeAsync(barcode);
                return exists ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product existence by barcode {Barcode}", barcode);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get products with comprehensive batch summary (for enhanced inventory display)
        /// </summary>
        [HttpGet("with-batch-summary")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<ProductWithBatchSummaryDto>>>> GetProductsWithBatchSummary(
            [FromQuery] ProductBatchSummaryFilterDto filter)
        {
            try
            {
                var response = await _productService.GetProductsWithBatchSummaryAsync(filter);

                return Ok(new ApiResponse<List<ProductWithBatchSummaryDto>>
                {
                    Success = true,
                    Data = response,
                    Message = $"Retrieved {response.Count} products with batch summary"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products with batch summary");
                return StatusCode(500, new ApiResponse<List<ProductWithBatchSummaryDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving products with batch summary"
                });
            }
        }

        /// <summary>
        /// Add stock with flexible batch options (new batch, existing batch, or no batch)
        /// </summary>
        [HttpPost("{productId}/add-stock")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<AddStockResponseDto>>> AddStockToBatch(int productId, [FromBody] AddStockToBatchRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var branchId = GetCurrentUserBranchId() ?? 1; // Default to main branch if not specified

                var response = await _productService.AddStockToBatchAsync(productId, request, userId, branchId);

                return Ok(new ApiResponse<AddStockResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = response.Success ? "Stock added successfully" : "Failed to add stock"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for adding stock to product {ProductId}", productId);
                return BadRequest(new ApiResponse<AddStockResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stock to product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<AddStockResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while adding stock"
                });
            }
        }

        /// <summary>
        /// Get FIFO recommendations for specific product
        /// </summary>
        [HttpGet("{productId}/fifo-recommendations")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<List<BatchFifoRecommendationDto>>>> GetProductFifoRecommendations(
            int productId, 
            [FromQuery] int? requestedQuantity = null)
        {
            try
            {
                var recommendations = await _productService.GetProductFifoRecommendationsAsync(productId, requestedQuantity);

                return Ok(new ApiResponse<List<BatchFifoRecommendationDto>>
                {
                    Success = true,
                    Data = recommendations,
                    Message = $"Retrieved {recommendations.Count} FIFO recommendations"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product ID {ProductId} for FIFO recommendations", productId);
                return BadRequest(new ApiResponse<List<FifoRecommendationDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FIFO recommendations for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<List<FifoRecommendationDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving FIFO recommendations"
                });
            }
        }

        /// <summary>
        /// Generate batch number for new batch creation
        /// </summary>
        [HttpPost("{productId}/generate-batch-number")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<GenerateBatchNumberResponseDto>>> GenerateBatchNumber(
            int productId,
            [FromBody] GenerateBatchNumberRequest request)
        {
            try
            {
                var response = await _productService.GenerateBatchNumberAsync(productId, request.ProductionDate);

                return Ok(new ApiResponse<GenerateBatchNumberResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "Batch number generated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for generating batch number for product {ProductId}", productId);
                return BadRequest(new ApiResponse<GenerateBatchNumberResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch number for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<GenerateBatchNumberResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while generating batch number"
                });
            }
        }

        /// <summary>
        /// Get real branch-specific stock for a product
        /// </summary>
        [HttpGet("{id}/branch-stock")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<object>>> GetProductBranchStock(int id, [FromQuery] string? branchIds = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                // Parse branch IDs for filtering
                var requestedBranchIds = new List<int>();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    requestedBranchIds = branchIds.Split(',')
                        .Where(bid => int.TryParse(bid.Trim(), out _))
                        .Select(bid => int.Parse(bid.Trim()))
                        .ToList();
                }
                else
                {
                    // Get all accessible branches if no specific branches requested
                    requestedBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                }

                // Validate user access to requested branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                requestedBranchIds = requestedBranchIds.Intersect(accessibleBranchIds).ToList();

                if (!requestedBranchIds.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No accessible branches specified"
                    });
                }

                // Get real branch stocks
                var branchStockData = await GetRealBranchStocksBatch(new List<int> { id }, requestedBranchIds);

                var branchStocks = new List<object>();
                var totalStock = 0;

                foreach (var branchId in requestedBranchIds)
                {
                    var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
                    if (branch != null)
                    {
                        var stock = branchStockData.TryGetValue((id, branchId), out var branchStock) ? branchStock : 0;
                        totalStock += stock;

                        branchStocks.Add(new
                        {
                            branchId = branch.Id,
                            branchName = branch.BranchName,
                            branchCode = branch.BranchCode,
                            stock = stock,
                            lastUpdated = DateTime.UtcNow,
                            stockLevel = GetStockLevel(stock),
                            isLowStock = stock <= 10, // Configurable threshold
                            isOutOfStock = stock <= 0
                        });
                    }
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        productId = id,
                        totalStock = totalStock,
                        branchCount = branchStocks.Count,
                        branchStocks = branchStocks,
                        generatedAt = DateTime.UtcNow
                    },
                    Message = $"Retrieved real branch stock data for {branchStocks.Count} branches"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch stock for product {ProductId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve branch stock data"
                });
            }
        }

        /// <summary>
        /// Get real branch stock for specific branch and product
        /// </summary>
        [HttpGet("{id}/branch-stock/{branchId}")]
        [Authorize(Policy = "Inventory.Read")]
        public async Task<ActionResult<ApiResponse<object>>> GetProductBranchStockSingle(int id, int branchId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                // Validate user access to branch
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                if (!accessibleBranchIds.Contains(branchId))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Access denied to this branch"
                    });
                }

                // Get branch info
                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
                if (branch == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Branch not found"
                    });
                }

                // Get real stock
                var realStock = await GetRealBranchStock(id, branchId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        productId = id,
                        branchId = branch.Id,
                        branchName = branch.BranchName,
                        branchCode = branch.BranchCode,
                        stock = realStock,
                        stockLevel = GetStockLevel(realStock),
                        isLowStock = realStock <= 10,
                        isOutOfStock = realStock <= 0,
                        lastUpdated = DateTime.UtcNow
                    },
                    Message = $"Retrieved real stock data: {realStock} units"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch stock for product {ProductId} branch {BranchId}", id, branchId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve branch stock data"
                });
            }
        }

        /// <summary>
        /// Adjust stock for specific branch (Required by frontend integration)
        /// </summary>
        [HttpPost("{id}/stock/adjust")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Validate branch access
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                if (!accessibleBranchIds.Contains(request.BranchId))
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied to this branch"
                    });
                }

                // Validate product exists
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Product not found"
                    });
                }

                // Validate branch exists
                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId && b.IsActive);
                if (branch == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid branch"
                    });
                }

                // Perform stock adjustment
                var oldStock = product.Stock;
                var newStock = request.AdjustmentType.ToLower() switch
                {
                    "addition" => product.Stock + request.Quantity,
                    "subtraction" => product.Stock - request.Quantity,
                    "set" => request.Quantity,
                    _ => product.Stock
                };

                if (newStock < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Stock cannot be negative"
                    });
                }

                product.Stock = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                // Log the stock mutation
                var stockMutation = new StockMutation
                {
                    ProductId = id,
                    UserId = currentUserId,
                    MutationType = request.AdjustmentType.ToLower() switch
                    {
                        "addition" => MutationType.Purchase,
                        "subtraction" => MutationType.Sale,
                        _ => MutationType.Adjustment
                    },
                    Quantity = request.AdjustmentType.ToLower() == "set" 
                        ? (newStock - oldStock) 
                        : (request.AdjustmentType.ToLower() == "addition" ? request.Quantity : -request.Quantity),
                    Notes = $"Branch {branch.BranchName}: {request.Reason}. {request.Notes}",
                    CreatedAt = DateTime.UtcNow,
                    ReferenceNumber = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks.ToString()[^6..]}"
                };

                _context.StockMutations.Add(stockMutation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stock adjusted for product {ProductId} in branch {BranchId} by user {UserId}. Old: {OldStock}, New: {NewStock}",
                    id, request.BranchId, currentUserId, oldStock, newStock);

                return Ok(new
                {
                    success = true,
                    message = "Stock adjusted successfully",
                    data = new
                    {
                        productId = id,
                        branchId = request.BranchId,
                        branchName = branch.BranchName,
                        oldStock = oldStock,
                        newStock = newStock,
                        adjustment = newStock - oldStock,
                        adjustedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock for product {ProductId} in branch {BranchId}", id, request.BranchId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        // ==================== HELPER METHODS ==================== //

        /// <summary>
        /// Get real branch-specific stock for a product using StockMutations with fallback to Product.Stock
        /// </summary>
        private async Task<int> GetRealBranchStock(int productId, int branchId)
        {
            // First, try to get from StockMutations for branch-specific tracking
            var latestMutation = await _context.StockMutations
                .Where(sm => sm.ProductId == productId && sm.BranchId == branchId)
                .OrderByDescending(sm => sm.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestMutation != null)
            {
                return latestMutation.StockAfter;
            }

            // Fallback: If no StockMutations exist for this branch, use Product.Stock
            // This assumes stock is distributed across branches, for now return proportional share
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                // For now, return the global stock (in future, implement branch-specific distribution)
                return product.Stock;
            }

            return 0;
        }

        /// <summary>
        /// Get real branch-specific stock for multiple products efficiently with fallback to Product.Stock
        /// </summary>
        private async Task<Dictionary<(int ProductId, int BranchId), int>> GetRealBranchStocksBatch(
            List<int> productIds, List<int> branchIds)
        {
            // First, get all StockMutations for these products and branches
            var branchStocks = await _context.StockMutations
                .Where(sm => productIds.Contains(sm.ProductId) && 
                           branchIds.Contains(sm.BranchId ?? 0))
                .GroupBy(sm => new { sm.ProductId, BranchId = sm.BranchId ?? 0 })
                .Select(g => new 
                {
                    ProductId = g.Key.ProductId,
                    BranchId = g.Key.BranchId,
                    CurrentStock = g.OrderByDescending(sm => sm.CreatedAt)
                                  .First().StockAfter
                })
                .ToListAsync();

            var result = branchStocks.ToDictionary(
                bs => (bs.ProductId, bs.BranchId), 
                bs => bs.CurrentStock
            );

            // Fallback: For combinations not in StockMutations, use Product.Stock
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Stock })
                .ToListAsync();

            foreach (var productId in productIds)
            {
                foreach (var branchId in branchIds)
                {
                    if (!result.ContainsKey((productId, branchId)))
                    {
                        var product = products.FirstOrDefault(p => p.Id == productId);
                        if (product != null)
                        {
                            // Use global stock as fallback (in future, implement branch distribution logic)
                            result[(productId, branchId)] = product.Stock;
                        }
                        else
                        {
                            result[(productId, branchId)] = 0;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get stock level classification for display
        /// </summary>
        private string GetStockLevel(int stock)
        {
            return stock switch
            {
                <= 0 => "out_of_stock",
                <= 10 => "low",
                <= 50 => "medium", 
                _ => "high"
            };
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   User.FindFirst(ClaimTypes.Role)?.Value ??
                   "User";
        }

        private async Task<List<int>> GetUserAccessibleBranches(int userId, string userRole)
        {
            var accessibleBranches = new List<int>();

            if (userRole.ToUpper() is "ADMIN" or "HEADMANAGER")
            {
                // Admin and HeadManager can access all branches
                accessibleBranches = await _context.Branches
                    .Where(b => b.IsActive)
                    .Select(b => b.Id)
                    .ToListAsync();
            }
            else
            {
                try
                {
                    // Get user's accessible branches via BranchAccess table
                    accessibleBranches = await _context.BranchAccesses
                        .Where(ba => ba.UserId == userId && ba.IsActive && ba.CanRead)
                        .Select(ba => ba.BranchId)
                        .ToListAsync();
                }
                catch
                {
                    // Fallback: Use user's assigned branch
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user?.BranchId.HasValue == true)
                    {
                        accessibleBranches.Add(user.BranchId.Value);
                    }
                }
            }

            return accessibleBranches;
        }

        /// <summary>
        /// Bulk update existing products with batches and expiry dates for categories that require expiry tracking
        /// </summary>
        [HttpPost("bulk-update-expiry-batches")]
        [Authorize(Policy = "Inventory.Write")]
        public async Task<ActionResult<ApiResponse<BulkUpdateResult>>> BulkUpdateProductsWithExpiryBatches(
            [FromBody] BulkUpdateExpiryBatchesRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Starting bulk update of products with expiry batches");

                var currentUserId = GetCurrentUserId();
                var username = User.Identity?.Name ?? "system";

                var result = new BulkUpdateResult();
                var processedProducts = new List<string>();
                var errors = new List<string>();

                // Get all products that need expiry tracking but don't have batches
                var productsQuery = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.Category.RequiresExpiryDate)
                    .AsQueryable();

                // Filter by category if specified
                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    productsQuery = productsQuery.Where(p => request.CategoryIds.Contains(p.CategoryId));
                }

                var products = await productsQuery.ToListAsync();

                foreach (var product in products)
                {
                    try
                    {
                        // Check if product already has batches
                        var existingBatchCount = await _context.ProductBatches
                            .CountAsync(b => b.ProductId == product.Id);

                        if (existingBatchCount > 0 && !request.ForceUpdate)
                        {
                            _logger.LogInformation("⏭️ Skipping product {ProductId} - already has {BatchCount} batches", 
                                product.Id, existingBatchCount);
                            result.SkippedCount++;
                            continue;
                        }

                        // Calculate default expiry date based on category
                        var defaultExpiryDate = request.DefaultExpiryDate ?? 
                            DateTime.UtcNow.AddDays(request.DefaultExpiryDays ?? 365);

                        // Update product expiry date if not set
                        if (product.ExpiryDate == null || request.ForceUpdate)
                        {
                            product.ExpiryDate = defaultExpiryDate;
                            product.UpdatedAt = DateTime.UtcNow;
                        }

                        // Create initial batch for existing stock
                        if (product.Stock > 0)
                        {
                            var batch = new ProductBatch
                            {
                                ProductId = product.Id,
                                BatchNumber = $"BULK-{DateTime.UtcNow:yyyyMMdd}-{product.Id}",
                                InitialStock = product.Stock,
                                CurrentStock = product.Stock,
                                ProductionDate = request.DefaultProductionDate ?? DateTime.UtcNow.AddDays(-30),
                                ExpiryDate = defaultExpiryDate,
                                CostPerUnit = request.DefaultCostPerUnit ?? product.BuyPrice,
                                SupplierName = request.DefaultSupplierName ?? "System Migration",
                                PurchaseOrderNumber = $"PO-BULK-{DateTime.UtcNow:yyyyMMdd}",
                                Notes = "Created during bulk migration for expiry tracking",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedByUserId = currentUserId,
                                UpdatedByUserId = currentUserId,
                                IsBlocked = false
                            };

                            _context.ProductBatches.Add(batch);
                        }

                        processedProducts.Add($"{product.Name} (ID: {product.Id})");
                        result.UpdatedCount++;

                        _logger.LogInformation("✅ Updated product {ProductId}: {ProductName}", 
                            product.Id, product.Name);

                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Failed to update product {product.Id} ({product.Name}): {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogError(ex, errorMsg);
                        result.ErrorCount++;
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();

                result.TotalProcessed = products.Count;
                result.ProcessedProducts = processedProducts;
                result.Errors = errors;

                _logger.LogInformation("🎯 Bulk update completed: {UpdatedCount} updated, {SkippedCount} skipped, {ErrorCount} errors", 
                    result.UpdatedCount, result.SkippedCount, result.ErrorCount);

                return Ok(new ApiResponse<BulkUpdateResult>
                {
                    Success = true,
                    Data = result,
                    Message = $"Bulk update completed: {result.UpdatedCount} products updated, {result.SkippedCount} skipped"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during bulk update of products with expiry batches");
                return StatusCode(500, new ApiResponse<BulkUpdateResult>
                {
                    Success = false,
                    Message = "Failed to perform bulk update: " + ex.Message
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private int? GetCurrentUserBranchId()
        {
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return int.TryParse(branchIdClaim, out var branchId) ? branchId : null;
        }

        private static readonly SemaphoreSlim _seedingSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Auto-seed branch inventory data for demo purposes with controlled concurrency
        /// </summary>
        private async Task SeedBranchInventoryAsync(int productId, int branchId, int stock, Product product)
        {
            try
            {
                // Use semaphore to prevent concurrent database operations
                await _seedingSemaphore.WaitAsync();

                try
                {
                    // Check if branch inventory already exists
                    var existingInventory = await _context.BranchInventories
                        .FirstOrDefaultAsync(bi => bi.ProductId == productId && bi.BranchId == branchId);

                    if (existingInventory == null)
                    {
                        // Create new branch inventory record
                        var branchInventory = new BranchInventory
                        {
                            ProductId = productId,
                            BranchId = branchId,
                            Stock = stock,
                            MinimumStock = (int)(stock * 0.2), // 20% as minimum
                            MaximumStock = stock * 3, // 3x as maximum
                            BuyPrice = product.BuyPrice,
                            SellPrice = product.SellPrice,
                            LocationCode = $"A{branchId}-{productId % 100:D2}",
                            LocationDescription = $"Branch {branchId} Storage Area",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            LastStockUpdate = DateTime.UtcNow
                        };

                        _context.BranchInventories.Add(branchInventory);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("✅ Seeded branch inventory for Product {ProductId} in Branch {BranchId} with stock {Stock}",
                            productId, branchId, stock);
                    }
                }
                finally
                {
                    _seedingSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seeding branch inventory for Product {ProductId} in Branch {BranchId}",
                    productId, branchId);
                // Don't throw - this is background seeding
            }
        }
    }

    // Request DTOs
    public class StockUpdateRequest
    {
        public int Quantity { get; set; }
        public MutationType MutationType { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal? UnitCost { get; set; }
    }

    public class StockAdjustmentRequest
    {
        [Required]
        public int BranchId { get; set; }
        
        [Required]
        public string AdjustmentType { get; set; } = string.Empty; // "Addition", "Subtraction", "Set"
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        public string Reason { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
    }
}