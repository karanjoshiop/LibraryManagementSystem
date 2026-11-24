namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record ReservationFormData(
        List<BookDto> Books,
        List<MemberDto> Members);
}
