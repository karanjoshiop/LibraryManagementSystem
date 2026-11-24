using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    public sealed class MembershipTypeInput
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 120)]
        public int DurationMonths { get; set; } = 12;

        [Range(0, 1000000)]
        public decimal Fee { get; set; }

        [Range(1, 100)]
        public int BorrowLimit { get; set; } = 3;

        [Range(1, 365)]
        public int LoanPeriodDays { get; set; } = 14;

        public bool IsActive { get; set; } = true;
    }
}
