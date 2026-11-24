namespace LibraryManagementSystem.Models
{
    public class LibrarySetting
    {
        public int LibrarySettingId { get; set; }

        public int ReservationHoldDays { get; set; } = 2;

        public decimal FinePerDay { get; set; } = 5;

        public decimal MaximumFine { get; set; } = 200;

        public decimal FineBorrowingLimit { get; set; } = 100;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}