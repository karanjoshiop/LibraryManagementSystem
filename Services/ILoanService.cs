using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface ILoanService
    {
        Task<List<LoanDto>> GetAllAsync();
        Task<LoanDto?> GetByIdAsync(long id);
        Task<LoanFormData> GetFormDataAsync();
        Task<BorrowInfo?> GetBorrowInfoAsync(int memberId);
        Task<int> CountOnLoanAsync();
        Task<int> CountOverdueAsync();
        Task<IssueResult> CreateAsync(LoanInput input);
        Task<bool> UpdateAsync(long id, LoanInput input);
        Task<bool> DeleteAsync(long id);
    }
}
