// Controllers/CategoryController.cs
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IMemoryCache _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService categoryService, IMemoryCache cache, ICacheInvalidationService cacheInvalidation, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        /// <summary>
        /// Get categories with filtering, sorting, and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<CategoryListDto>> GetCategories([FromQuery] CategoryFilterDto filter)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"categories_{filter?.SearchTerm ?? "all"}_{filter?.Page}_{filter?.PageSize}_{filter?.SortBy}";

                if (_cache.TryGetValue(cacheKey, out CategoryListDto? cachedCategories))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved categories from cache for user {Username}", username);
                    return Ok(cachedCategories);
                }

                _logger.LogInformation("🔄 Cache MISS: Fetching categories from database for user {Username} with filter: {@Filter}", username, filter);

                var result = await _categoryService.GetCategoriesAsync(filter);

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45), // Categories cache for 45 minutes (changes infrequently)
                    SlidingExpiration = TimeSpan.FromMinutes(15),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored categories in cache");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "An error occurred while retrieving categories" });
            }
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid category ID" });
                }

                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"category_by_id_{id}";

                if (_cache.TryGetValue(cacheKey, out CategoryDto? cachedCategory))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved category from cache for ID {CategoryId}", id);
                    return Ok(cachedCategory);
                }

                _logger.LogInformation("🔄 Cache MISS: Fetching category from database for ID {CategoryId}", id);

                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60), // Individual categories cache for 1 hour
                    SlidingExpiration = TimeSpan.FromMinutes(20),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, category, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored category in cache for ID {CategoryId}", id);

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the category" });
            }
        }

        /// <summary>
        /// Create new category
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")] // Only Admin and Manager can create categories
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} creating category: {CategoryName}", username, createDto.Name);

                var category = await _categoryService.CreateCategoryAsync(createDto);

                // ✅ CACHE INVALIDATION: Clear category-related caches after creation
                _cacheInvalidation.InvalidateByPattern("categories_*");
                _cacheInvalidation.InvalidateByPattern("pos_products_*"); // Product filtering might change
                _cacheInvalidation.InvalidateByPattern("products_*");

                _logger.LogInformation("🗑️ Cache invalidated after category creation: {CategoryName} (ID: {CategoryId})",
                    category.Name, category.Id);

                // Log activity
                _logger.LogInformation("Category created successfully by {Username}: {CategoryName} (ID: {CategoryId})",
                    username, category.Name, category.Id);

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating category");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { message = "An error occurred while creating the category" });
            }
        }

        /// <summary>
        /// Update existing category
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")] // Only Admin and Manager can update categories
        public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateDto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid category ID" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} updating category {CategoryId}", username, id);

                var category = await _categoryService.UpdateCategoryAsync(id, updateDto);
                if (category == null)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                // ✅ CACHE INVALIDATION: Clear category-related caches after update
                _cacheInvalidation.InvalidateByPattern("categories_*");
                _cacheInvalidation.InvalidateByPattern($"category_by_id_{id}");
                _cacheInvalidation.InvalidateByPattern("pos_products_*"); // Product filtering might change
                _cacheInvalidation.InvalidateByPattern("products_*");

                _logger.LogInformation("🗑️ Cache invalidated after category update: {CategoryId}", id);
                _logger.LogInformation("Category updated successfully by {Username}: {CategoryName} (ID: {CategoryId})",
                    username, category.Name, category.Id);

                return Ok(category);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating category {CategoryId}", id);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the category" });
            }
        }

        /// <summary>
        /// Delete category
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can delete categories
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid category ID" });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} deleting category {CategoryId}", username, id);

                var result = await _categoryService.DeleteCategoryAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                _logger.LogInformation("Category deleted successfully by {Username}: ID {CategoryId}", username, id);

                return Ok(new { message = "Category deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while deleting category {CategoryId}", id);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the category" });
            }
        }

        /// <summary>
        /// Check if category name exists
        /// </summary>
        [HttpGet("check-name")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<bool>> CheckCategoryName([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { message = "Category name is required" });
                }

                var exists = await _categoryService.CategoryExistsAsync(name, excludeId);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking category name: {CategoryName}", name);
                return StatusCode(500, new { message = "An error occurred while checking the category name" });
            }
        }

        /// <summary>
        /// Get all categories (simple list for dropdowns)
        /// </summary>
        [HttpGet("simple")]
        public async Task<ActionResult<List<CategoryDto>>> GetCategoriesSimple()
        {
            try
            {
                var filter = new CategoryFilterDto
                {
                    Page = 1,
                    PageSize = 1000, // Get all categories
                    SortBy = "name",
                    SortOrder = "asc"
                };

                var result = await _categoryService.GetCategoriesAsync(filter);
                return Ok(result.Categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simple categories list");
                return StatusCode(500, new { message = "An error occurred while retrieving categories" });
            }
        }

        // ==================== EXPIRY-RELATED ENDPOINTS ==================== //

        /// <summary>
        /// Get all categories that require expiry date tracking
        /// </summary>
        [HttpGet("with-expiry")]
        public async Task<ActionResult<List<CategoryWithExpiryDto>>> GetCategoriesRequiringExpiry()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested categories requiring expiry", username);

                var categories = await _categoryService.GetCategoriesRequiringExpiryAsync();
                
                _logger.LogInformation("Found {Count} categories requiring expiry tracking", categories.Count);
                
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories requiring expiry");
                return StatusCode(500, new { message = "An error occurred while retrieving expiry categories" });
            }
        }

        /// <summary>
        /// Check if a specific category requires expiry date tracking
        /// </summary>
        [HttpGet("{id}/requires-expiry")]
        public async Task<ActionResult<object>> CheckCategoryRequiresExpiry(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid category ID" });
                }

                var requiresExpiry = await _categoryService.CategoryRequiresExpiryAsync(id);
                
                return Ok(new { 
                    categoryId = id, 
                    requiresExpiry = requiresExpiry 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiry requirement for category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while checking expiry requirement" });
            }
        }

        /// <summary>
        /// Get categories with expiry statistics (products counts, expiring, expired)
        /// </summary>
        [HttpGet("expiry-stats")]
        [Authorize(Roles = "Admin,Manager")] // Only managers can view detailed stats
        public async Task<ActionResult<List<CategoryDto>>> GetCategoriesWithExpiryStats()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} requested categories with expiry statistics", username);

                var categories = await _categoryService.GetCategoriesWithExpiryStatsAsync();
                
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories with expiry stats");
                return StatusCode(500, new { message = "An error occurred while retrieving expiry statistics" });
            }
        }

        /// <summary>
        /// Update expiry requirements for multiple categories (batch operation)
        /// </summary>
        [HttpPost("update-expiry-requirements")]
        [Authorize(Roles = "Admin")] // Only Admin can update expiry requirements
        public async Task<ActionResult<object>> UpdateCategoryExpiryRequirements([FromBody] Dictionary<int, bool> categoryExpiryMap)
        {
            try
            {
                if (categoryExpiryMap == null || !categoryExpiryMap.Any())
                {
                    return BadRequest(new { message = "Category expiry mapping is required" });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation("User {Username} updating expiry requirements for {Count} categories", 
                    username, categoryExpiryMap.Count);

                var success = await _categoryService.UpdateCategoryExpiryRequirementsAsync(categoryExpiryMap);
                
                if (success)
                {
                    _logger.LogInformation("Successfully updated expiry requirements for categories by {Username}", username);
                    return Ok(new { 
                        message = "Category expiry requirements updated successfully",
                        updatedCount = categoryExpiryMap.Count 
                    });
                }
                else
                {
                    return BadRequest(new { message = "No categories were updated" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category expiry requirements");
                return StatusCode(500, new { message = "An error occurred while updating expiry requirements" });
            }
        }

        /// <summary>
        /// Get categories filtered by expiry requirement (for product forms)
        /// </summary>
        [HttpGet("by-expiry-requirement")]
        public async Task<ActionResult<List<CategoryDto>>> GetCategoriesByExpiryRequirement([FromQuery] bool requiresExpiry = true)
        {
            try
            {
                var filter = new CategoryFilterDto
                {
                    RequiresExpiryDate = requiresExpiry,
                    Page = 1,
                    PageSize = 1000,
                    SortBy = "name",
                    SortOrder = "asc"
                };

                var result = await _categoryService.GetCategoriesAsync(filter);
                
                _logger.LogInformation("Found {Count} categories with expiry requirement: {RequiresExpiry}", 
                    result.Categories.Count, requiresExpiry);
                
                return Ok(result.Categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories by expiry requirement");
                return StatusCode(500, new { message = "An error occurred while retrieving categories" });
            }
        }
    }
}