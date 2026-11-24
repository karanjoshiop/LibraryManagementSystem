using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
        Task<int> CountAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<bool> CreateAsync(CategoryInput input);
        Task<bool> UpdateAsync(int id, CategoryInput input);
        Task<bool> DeleteAsync(int id);
    }
}