// Controllers/CategoryController.cs
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
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
                _logger.LogInformation("User {Username} requested categories with filter: {@Filter}", username, filter);

                var result = await _categoryService.GetCategoriesAsync(filter);
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

                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

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
    }
}