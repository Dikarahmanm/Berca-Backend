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
    }
}