using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IMembershipTypeService
    {
        Task<List<MembershipTypeDto>> GetAllAsync();
        Task<MembershipTypeDto?> GetByIdAsync(int id);
        Task<bool> CreateAsync(MembershipTypeInput input);
        Task<bool> UpdateAsync(int id, MembershipTypeInput input);
        Task<bool> DeleteAsync(int id);
    }
}
