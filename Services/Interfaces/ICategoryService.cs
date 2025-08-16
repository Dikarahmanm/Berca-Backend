using Berca_Backend.DTOs;

namespace Berca_Backend.Services
{
    public interface ICategoryService
    {
        Task<CategoryListDto> GetCategoriesAsync(CategoryFilterDto filter);
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto);
        Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(string name, int? excludeId = null);

        // ==================== EXPIRY-RELATED METHODS ==================== //
        
        /// <summary>
        /// Get all categories that require expiry date tracking
        /// </summary>
        Task<List<CategoryWithExpiryDto>> GetCategoriesRequiringExpiryAsync();
        
        /// <summary>
        /// Check if a category requires expiry date tracking
        /// </summary>
        Task<bool> CategoryRequiresExpiryAsync(int categoryId);
        
        /// <summary>
        /// Get categories with expiry statistics
        /// </summary>
        Task<List<CategoryDto>> GetCategoriesWithExpiryStatsAsync();
        
        /// <summary>
        /// Update expiry requirements for multiple categories (batch operation)
        /// </summary>
        Task<bool> UpdateCategoryExpiryRequirementsAsync(Dictionary<int, bool> categoryExpiryMap);
    }
}