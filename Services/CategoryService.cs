// Services/CategoryService.cs
using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Berca_Backend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(AppDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CategoryListDto> GetCategoriesAsync(CategoryFilterDto filter)
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                           (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
                }

                // Apply color filter
                if (!string.IsNullOrWhiteSpace(filter.Color))
                {
                    query = query.Where(c => c.Color == filter.Color);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, filter.SortBy, filter.SortOrder);

                // Apply pagination
                var categories = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Color = c.Color,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        ProductCount = 0 // Set to 0 for now
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

                return new CategoryListDto
                {
                    Categories = categories,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories with filter: {@Filter}", filter);
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Where(c => c.Id == id)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Color = c.Color,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        ProductCount = 0
                    })
                    .FirstOrDefaultAsync();

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID: {CategoryId}", id);
                throw;
            }
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto)
        {
            try
            {
                // Check if category name already exists
                if (await CategoryExistsAsync(createDto.Name))
                {
                    throw new InvalidOperationException($"Category with name '{createDto.Name}' already exists");
                }

                var category = new Category
                {
                    Name = createDto.Name.Trim(),
                    Color = createDto.Color.ToUpperInvariant(),
                    Description = createDto.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category created successfully: {CategoryName} (ID: {CategoryId})",
                    category.Name, category.Id);

                return new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Color = category.Color,
                    Description = category.Description,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {@CreateDto}", createDto);
                throw;
            }
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return null;
                }

                // Check if new name conflicts with existing categories (excluding current)
                if (await CategoryExistsAsync(updateDto.Name, id))
                {
                    throw new InvalidOperationException($"Category with name '{updateDto.Name}' already exists");
                }

                category.Name = updateDto.Name.Trim();
                category.Color = updateDto.Color.ToUpperInvariant();
                category.Description = updateDto.Description?.Trim();
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Category updated successfully: {CategoryName} (ID: {CategoryId})",
                    category.Name, category.Id);

                return new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Color = category.Color,
                    Description = category.Description,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}: {@UpdateDto}", id, updateDto);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return false;
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category deleted successfully: {CategoryName} (ID: {CategoryId})",
                    category.Name, category.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                throw;
            }
        }

        public async Task<bool> CategoryExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                var query = _context.Categories.Where(c => c.Name.ToLower() == name.ToLower());

                if (excludeId.HasValue)
                {
                    query = query.Where(c => c.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if category exists: {CategoryName}", name);
                throw;
            }
        }

        private static IQueryable<Category> ApplySorting(IQueryable<Category> query, string sortBy, string sortOrder)
        {
            Expression<Func<Category, object>> keySelector = sortBy.ToLower() switch
            {
                "name" => c => c.Name,
                "color" => c => c.Color,
                "createdat" => c => c.CreatedAt,
                "updatedat" => c => c.UpdatedAt,
                _ => c => c.Name
            };

            return sortOrder.ToLower() == "desc"
                ? query.OrderByDescending(keySelector)
                : query.OrderBy(keySelector);
        }
    }
}