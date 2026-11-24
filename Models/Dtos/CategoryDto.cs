namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record CategoryDto(
        int CategoryId,
        string Name,
        string? Description);
}
