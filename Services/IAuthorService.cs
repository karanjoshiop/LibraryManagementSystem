using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IAuthorService
    {
        Task<List<AuthorDto>> GetAllAsync();
        Task<int> CountAsync();
        Task<AuthorDto?> GetByIdAsync(int id);
        Task<bool> CreateAsync(AuthorInput input);
        Task<bool> UpdateAsync(int id, AuthorInput input);
        Task<bool> DeleteAsync(int id);
    }
}
