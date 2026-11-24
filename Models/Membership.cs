using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Membership
    {
        public int MembershipId { get; set; }

        public int MemberId { get; set; }

        public int MembershipTypeId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Member Member { get; set; } = null!;

        public MembershipType MembershipType { get; set; } = null!;
    }
}