using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IMembershipService
    {
        Task<List<MembershipDto>> GetAllAsync();
        Task<MembershipDto?> GetByIdAsync(int id);
        Task<MembershipFormData> GetFormDataAsync();
        Task<int> CountActiveAsync();
        Task<bool> CreateAsync(MembershipInput input);
        Task<bool> UpdateAsync(int id, MembershipInput input);
        Task<bool> DeleteAsync(int id);

        // Lifecycle helpers (free plan, upgrades, renewal, expiry).
        Task<bool> UpgradeAsync(int memberId, int newMembershipTypeId);
        Task<bool> RenewAsync(int membershipId);
        Task<int> RefreshExpiriesAsync();
    }
}
