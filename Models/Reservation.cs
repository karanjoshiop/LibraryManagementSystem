using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Reservation
    {
        public long ReservationId { get; set; }

        public int BookId { get; set; }

        public int MemberId { get; set; }

        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Book Book { get; set; } = null!;

        public Member Member { get; set; } = null!;
    }
}