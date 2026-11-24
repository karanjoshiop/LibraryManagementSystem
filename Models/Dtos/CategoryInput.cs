using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    public sealed class CategoryInput
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}