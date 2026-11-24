using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class CategoryService : ICategoryService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public CategoryService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.CategoryId, c.Name, c.Description))
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Categories.CountAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Categories
                .AsNoTracking()
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDto(c.CategoryId, c.Name, c.Description))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(CategoryInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var name = input.Name.Trim();
            var lowered = name.ToLower();

            // Literal case-insensitive match: user input must not be treated as
            // an ILIKE pattern (a name of "%" would otherwise match everything).
            if (await db.Categories.AnyAsync(c => c.Name.ToLower() == lowered))
                return false;

            db.Categories.Add(new Category
            {
                Name = name,
                Description = input.Description,
                CreatedAt = DateTime.UtcNow
            });

            return await db.TrySaveAsync();
        }

        public async Task<bool> UpdateAsync(int id, CategoryInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            // Query by key (not FindAsync) so the soft-delete filter is applied.
            var entity = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (entity is null) return false;

            var name = input.Name.Trim();
            var lowered = name.ToLower();

            if (await db.Categories.AnyAsync(c =>
                    c.CategoryId != id && c.Name.ToLower() == lowered))
                return false;

            entity.Name = name;
            entity.Description = input.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (entity is null) return false;

            // Ignore parent filters: a book is live if the book itself is not deleted,
            // even when its category/author navigation happens to be hidden.
            if (await db.Books.IgnoreQueryFilters()
                    .AnyAsync(b => b.CategoryId == id && b.DeletedAt == null))
                return false;

            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }
    }
}
