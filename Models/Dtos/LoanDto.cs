namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record LoanDto(
        long LoanId,
        int BookId,
        string BookTitle,
        int MemberId,
        string MemberName,
        DateOnly IssueDate,
        DateOnly DueDate,
        DateOnly? ReturnDate,
        decimal FineAmount,
        decimal FinePaidAmount,
        string Status);
}
