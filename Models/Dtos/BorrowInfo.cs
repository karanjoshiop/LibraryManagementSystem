namespace LibraryManagementSystem.Models.Dtos
{
    /// <summary>
    /// Snapshot of the rules that apply when issuing a book to a member.
    /// Used by the loan form to pre-fill the due date and explain decisions.
    /// </summary>
    public sealed record BorrowInfo(
        string? PlanName,
        int LoanPeriodDays,
        int BorrowLimit,
        int OpenLoans,
        DateOnly? ActiveUntil,
        bool HasActiveMembership);
}
