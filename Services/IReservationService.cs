using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IReservationService
    {
        Task<List<ReservationDto>> GetAllAsync();
        Task<ReservationDto?> GetByIdAsync(long id);
        Task<ReservationFormData> GetFormDataAsync();
        Task<int> CountPendingAsync();
        Task<int> ExpireOldReservationsAsync();
        Task<IssueResult> FulfillAsync(long reservationId);
        Task<IssueResult> CreateAsync(ReservationInput input);
        Task<bool> UpdateAsync(long id, ReservationInput input);
        Task<bool> DeleteAsync(long id);
    }
}
