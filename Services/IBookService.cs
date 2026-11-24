using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IBookService
    {
        Task<List<BookDto>> GetAllAsync();
        Task<int> CountAsync();
        Task<BookDto?> GetByIdAsync(int id);
        Task<BookFormData> GetFormDataAsync();
        Task<bool> CreateAsync(BookInput input);
        Task<bool> UpdateAsync(int id, BookInput input);
        Task<bool> DeleteAsync(int id);
    }
}
