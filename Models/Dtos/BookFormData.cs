namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record BookFormData(
        List<CategoryDto> Categories,
        List<AuthorDto> Authors);
}
