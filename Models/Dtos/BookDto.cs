namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record BookDto(
        int BookId,
        string Title,
        string ISBN,
        int CategoryId,
        string CategoryName,
        int TotalCopies,
        List<int> AuthorIds,
        List<string> AuthorNames,
        int AvailableCopies = 0);
}
