using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    public sealed class MemberInput
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
