using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record LibrarySettingDto(
        int LibrarySettingId,
        int ReservationHoldDays,
        decimal FinePerDay,
        decimal MaximumFine,
        decimal FineBorrowingLimit,
        DateTime? UpdatedAt);

    public sealed class LibrarySettingInput
    {
        [Range(1, 365, ErrorMessage = "Reservation hold must be 1-365 days.")]
        public int ReservationHoldDays { get; set; } = 2;

        [Range(0, 10000, ErrorMessage = "Fine per day must be between 0 and 10000.")]
        public decimal FinePerDay { get; set; } = 5m;

        [Range(0, 100000, ErrorMessage = "Maximum fine must be between 0 and 100000.")]
        public decimal MaximumFine { get; set; } = 200m;

        [Range(0, 100000, ErrorMessage = "Borrowing fine limit must be between 0 and 100000.")]
        public decimal FineBorrowingLimit { get; set; } = 100m;
    }
}
