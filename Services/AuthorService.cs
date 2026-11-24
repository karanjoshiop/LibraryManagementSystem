using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class AuthorService : IAuthorService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public AuthorService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<AuthorDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Authors
                .AsNoTracking()
                .OrderBy(a => a.FullName)
                .Select(a => new AuthorDto(a.AuthorId, a.FullName, a.Bio))
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Authors.CountAsync();
        }

        public async Task<AuthorDto?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Authors
                .AsNoTracking()
                .Where(a => a.AuthorId == id)
                .Select(a => new AuthorDto(a.AuthorId, a.FullName, a.Bio))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(AuthorInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var name = input.FullName.Trim();
            var lowered = name.ToLower();

            if (await db.Authors.AnyAsync(a => a.FullName.ToLower() == lowered))
                return false;

            db.Authors.Add(new Author
            {
                FullName = name,
                Bio = input.Bio,
                CreatedAt = DateTime.UtcNow
            });

            return await db.TrySaveAsync();
        }

        public async Task<bool> UpdateAsync(int id, AuthorInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Authors.FirstOrDefaultAsync(a => a.AuthorId == id);
            if (entity is null) return false;

            var name = input.FullName.Trim();
            var lowered = name.ToLower();

            if (await db.Authors.AnyAsync(a =>
                    a.AuthorId != id && a.FullName.ToLower() == lowered))
                return false;

            entity.FullName = name;
            entity.Bio = input.Bio;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Authors.FirstOrDefaultAsync(a => a.AuthorId == id);
            if (entity is null) return false;

            if (await db.BookAuthors.IgnoreQueryFilters()
                    .AnyAsync(ba => ba.AuthorId == id && ba.DeletedAt == null))
                return false;

            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }
    }
}
