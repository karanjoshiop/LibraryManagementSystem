using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    [EndDateAfterStartDate]
    public sealed class MembershipInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a member.")]
        public int MemberId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a membership type.")]
        public int MembershipTypeId { get; set; }

        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddYears(1);

        [Required, MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Active";
    }
}
