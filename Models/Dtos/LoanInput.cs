namespace LibraryManagementSystem.Models.Dtos
{
    public sealed class LoanInput
    {
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddDays(14);
        public DateOnly? ReturnDate { get; set; }
        public decimal FineAmount { get; set; }
        public decimal FinePaidAmount { get; set; }
    }
}
