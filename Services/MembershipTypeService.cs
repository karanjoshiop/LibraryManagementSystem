using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class MembershipTypeService : IMembershipTypeService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public MembershipTypeService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<MembershipTypeDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.MembershipTypes
                .AsNoTracking()
                .OrderBy(mt => mt.Name)
                .Select(mt => new MembershipTypeDto(
                    mt.MembershipTypeId, mt.Name, mt.DurationMonths,
                    mt.Fee, mt.BorrowLimit, mt.LoanPeriodDays, mt.IsActive))
                .ToListAsync();
        }

        public async Task<MembershipTypeDto?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.MembershipTypes
                .AsNoTracking()
                .Where(mt => mt.MembershipTypeId == id)
                .Select(mt => new MembershipTypeDto(
                    mt.MembershipTypeId, mt.Name, mt.DurationMonths,
                    mt.Fee, mt.BorrowLimit, mt.LoanPeriodDays, mt.IsActive))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(MembershipTypeInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var name = input.Name.Trim();
            var lowered = name.ToLower();

            if (await db.MembershipTypes.AnyAsync(mt => mt.Name.ToLower() == lowered))
                return false;

            db.MembershipTypes.Add(new MembershipType
            {
                Name = name,
                DurationMonths = input.DurationMonths,
                Fee = input.Fee,
                BorrowLimit = input.BorrowLimit,
                LoanPeriodDays = input.LoanPeriodDays,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow
            });

            return await db.TrySaveAsync();
        }

        public async Task<bool> UpdateAsync(int id, MembershipTypeInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.MembershipTypes
                .FirstOrDefaultAsync(mt => mt.MembershipTypeId == id);
            if (entity is null) return false;

            var name = input.Name.Trim();
            var lowered = name.ToLower();

            if (await db.MembershipTypes.AnyAsync(mt =>
                    mt.MembershipTypeId != id && mt.Name.ToLower() == lowered))
                return false;

            entity.Name = name;
            entity.DurationMonths = input.DurationMonths;
            entity.Fee = input.Fee;
            entity.BorrowLimit = input.BorrowLimit;
            entity.LoanPeriodDays = input.LoanPeriodDays;
            entity.IsActive = input.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.MembershipTypes
                .FirstOrDefaultAsync(mt => mt.MembershipTypeId == id);
            if (entity is null) return false;

            if (await db.Memberships.IgnoreQueryFilters()
                    .AnyAsync(m => m.MembershipTypeId == id && m.DeletedAt == null))
                return false;

            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }
    }
}
