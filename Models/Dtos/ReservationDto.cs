namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record ReservationDto(
        long ReservationId,
        int BookId,
        string BookTitle,
        int MemberId,
        string MemberName,
        DateTime ReservedAt,
        string Status);
}
