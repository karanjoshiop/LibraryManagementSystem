using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IMemberService
    {
        Task<List<MemberDto>> GetAllAsync();
        Task<int> CountAsync();
        Task<MemberDto?> GetByIdAsync(int id);
        Task<bool> CreateAsync(MemberInput input);
        Task<bool> UpdateAsync(int id, MemberInput input);
        Task<bool> DeleteAsync(int id);
    }
}
