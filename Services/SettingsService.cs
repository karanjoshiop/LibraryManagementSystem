using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private const int SingleSettingsRowId = 1;

        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public SettingsService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<LibrarySettingDto?> GetAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.LibrarySettings
                .AsNoTracking()
                .Where(s => s.LibrarySettingId == SingleSettingsRowId)
                .Select(s => new LibrarySettingDto(
                    s.LibrarySettingId, s.ReservationHoldDays,
                    s.FinePerDay, s.MaximumFine, s.FineBorrowingLimit, s.UpdatedAt))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(LibrarySettingInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.LibrarySettings
                .FirstOrDefaultAsync(s => s.LibrarySettingId == SingleSettingsRowId);
            if (entity is null) return false;

            entity.ReservationHoldDays = input.ReservationHoldDays;
            entity.FinePerDay = input.FinePerDay;
            entity.MaximumFine = input.MaximumFine;
            entity.FineBorrowingLimit = input.FineBorrowingLimit;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }
    }
}
