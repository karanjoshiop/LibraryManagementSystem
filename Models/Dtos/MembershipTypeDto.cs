namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record MembershipTypeDto(
        int MembershipTypeId,
        string Name,
        int DurationMonths,
        decimal Fee,
        int BorrowLimit,
        int LoanPeriodDays,
        bool IsActive);
}
