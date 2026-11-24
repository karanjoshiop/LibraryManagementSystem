using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class MembershipType
    {
        public int MembershipTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int DurationMonths { get; set; }

        public decimal Fee { get; set; }

        public int BorrowLimit { get; set; }

        public int LoanPeriodDays { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}