namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record AuthorDto(
        int AuthorId,
        string FullName,
        string? Bio);
}
