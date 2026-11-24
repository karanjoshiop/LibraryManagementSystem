namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record LoanFormData(
        List<BookDto> Books,
        List<MemberDto> Members);
}
