namespace LibraryManagementSystem.Models
{
    public class Loan
    {
        public long LoanId { get; set; }

        public int BookId { get; set; }

        public int MemberId { get; set; }

        public DateOnly IssueDate { get; set; }

        public DateOnly DueDate { get; set; }

        public DateOnly? ReturnDate { get; set; }

        public decimal FineAmount { get; set; } = 0;

        public decimal FinePaidAmount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Book Book { get; set; } = null!;

        public Member Member { get; set; } = null!;
    }
}