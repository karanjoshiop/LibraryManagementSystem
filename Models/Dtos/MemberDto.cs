namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record MemberDto(
        int MemberId,
        string FullName,
        string Email,
        string? Phone,
        bool IsActive,
        string? PlanName = null,
        DateOnly? MembershipEndDate = null,
        bool MembershipExpired = false,
        int OpenLoans = 0,
        decimal OutstandingFine = 0m);
}
