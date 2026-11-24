namespace LibraryManagementSystem.Models.Dtos
{
    public sealed class ReservationInput
    {
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
