using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface ISettingsService
    {
        Task<LibrarySettingDto?> GetAsync();
        Task<bool> UpdateAsync(LibrarySettingInput input);
    }
}
