using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    public sealed class BookInput
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(13)]
        public string ISBN { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalCopies { get; set; }

        public List<int> AuthorIds { get; set; } = new();
    }
}
