using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class BookService : IBookService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public BookService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<BookDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var books = await db.Books
                .AsNoTracking()
                .OrderBy(b => b.Title)
                .Select(b => new BookDto(
                    b.BookId, b.Title, b.ISBN, b.CategoryId,
                    b.Category.Name, b.TotalCopies,
                    new List<int>(), new List<string>(), 0))
                .ToListAsync();

            var bookIds = books.Select(b => b.BookId).ToList();

            var issuedByBook = (await db.Loans
                    .AsNoTracking()
                    .Where(l => l.ReturnDate == null)
                    .GroupBy(l => l.BookId)
                    .Select(g => new { BookId = g.Key, Count = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.BookId, x => x.Count);

            var linksByBook = (await db.BookAuthors
                    .AsNoTracking()
                    .Where(ba => bookIds.Contains(ba.BookId))
                    .Select(ba => new
                    {
                        ba.BookId,
                        ba.AuthorId,
                        ba.Author.FullName
                    })
                    .ToListAsync())
                .ToLookup(l => l.BookId);

            var result = new List<BookDto>(books.Count);
            foreach (var book in books)
            {
                issuedByBook.TryGetValue(book.BookId, out var issued);
                var enriched = book with { AvailableCopies = book.TotalCopies - issued };

                foreach (var link in linksByBook[book.BookId])
                {
                    enriched.AuthorIds.Add(link.AuthorId);
                    enriched.AuthorNames.Add(link.FullName);
                }

                result.Add(enriched);
            }

            return result;
        }

        public async Task<int> CountAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Books.CountAsync();
        }

        public async Task<BookDto?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var book = await db.Books
                .AsNoTracking()
                .Where(b => b.BookId == id)
                .Select(b => new BookDto(
                    b.BookId, b.Title, b.ISBN, b.CategoryId,
                    b.Category.Name, b.TotalCopies,
                    new List<int>(), new List<string>(), 0))
                .FirstOrDefaultAsync();

            if (book is null) return null;

            var issued = await db.Loans
                .CountAsync(l => l.BookId == id && l.ReturnDate == null);
            book = book with { AvailableCopies = book.TotalCopies - issued };

            var links = await db.BookAuthors
                .AsNoTracking()
                .Where(ba => ba.BookId == id)
                .Select(ba => new { ba.AuthorId, ba.Author.FullName })
                .ToListAsync();

            foreach (var link in links)
            {
                book.AuthorIds.Add(link.AuthorId);
                book.AuthorNames.Add(link.FullName);
            }

            return book;
        }

        public async Task<BookFormData> GetFormDataAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.CategoryId, c.Name, c.Description))
                .ToListAsync();

            var authors = await db.Authors
                .AsNoTracking()
                .OrderBy(a => a.FullName)
                .Select(a => new AuthorDto(a.AuthorId, a.FullName, a.Bio))
                .ToListAsync();

            return new BookFormData(categories, authors);
        }

        public async Task<bool> CreateAsync(BookInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var isbn = input.ISBN.Trim();
            var authorIds = NormalizeAuthors(input.AuthorIds);
            if (authorIds.Count == 0) return false;

            if (await db.Books.AnyAsync(b => b.ISBN == isbn)) return false;
            if (!await db.Categories.AnyAsync(c => c.CategoryId == input.CategoryId)) return false;
            if (await db.Authors.CountAsync(a => authorIds.Contains(a.AuthorId)) != authorIds.Count)
                return false;

            await using var tx = await db.Database.BeginTransactionAsync();

            var book = new Book
            {
                Title = input.Title.Trim(),
                ISBN = isbn,
                CategoryId = input.CategoryId,
                TotalCopies = input.TotalCopies,
                CreatedAt = DateTime.UtcNow
            };

            db.Books.Add(book);

            try
            {
                await db.SaveChangesAsync(); // get generated BookId

                // EF does NOT create BookAuthor rows by itself - insert explicitly.
                foreach (var authorId in authorIds)
                {
                    db.BookAuthors.Add(new BookAuthor
                    {
                        BookId = book.BookId,
                        AuthorId = authorId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                db.DetachChanges();
                await tx.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> UpdateAsync(int id, BookInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var book = await db.Books.FirstOrDefaultAsync(b => b.BookId == id);
            if (book is null) return false;

            var isbn = input.ISBN.Trim();
            var authorIds = NormalizeAuthors(input.AuthorIds);
            if (authorIds.Count == 0) return false;

            if (await db.Books.AnyAsync(b => b.BookId != id && b.ISBN == isbn)) return false;
            if (!await db.Categories.AnyAsync(c => c.CategoryId == input.CategoryId)) return false;
            // Filtered query rejects deleted authors, including on revival.
            if (await db.Authors.CountAsync(a => authorIds.Contains(a.AuthorId)) != authorIds.Count)
                return false;

            var issuedCopies = await db.Loans.IgnoreQueryFilters()
                .CountAsync(l => l.BookId == id && l.ReturnDate == null && l.DeletedAt == null);
            if (input.TotalCopies < issuedCopies) return false;

            book.Title = input.Title.Trim();
            book.ISBN = isbn;
            book.CategoryId = input.CategoryId;
            book.TotalCopies = input.TotalCopies;
            book.UpdatedAt = DateTime.UtcNow;

            // Include soft-deleted links so we can revive instead of re-inserting
            // (the composite PK (BookId, AuthorId) cannot be duplicated).
            var existingLinks = await db.BookAuthors
                .IgnoreQueryFilters()
                .Where(ba => ba.BookId == id)
                .ToListAsync();

            foreach (var link in existingLinks)
            {
                if (authorIds.Contains(link.AuthorId))
                {
                    if (link.DeletedAt is not null)
                    {
                        link.DeletedAt = null; // revive
                        link.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (link.DeletedAt is null)
                {
                    link.DeletedAt = DateTime.UtcNow; // soft delete
                    link.UpdatedAt = DateTime.UtcNow;
                }
            }

            var existingIds = existingLinks.Select(l => l.AuthorId).ToHashSet();
            foreach (var authorId in authorIds.Where(a => !existingIds.Contains(a)))
            {
                db.BookAuthors.Add(new BookAuthor
                {
                    BookId = id,
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var book = await db.Books.FirstOrDefaultAsync(b => b.BookId == id);
            if (book is null) return false;

            if (await db.Loans.IgnoreQueryFilters()
                    .AnyAsync(l => l.BookId == id && l.DeletedAt == null)) return false;
            if (await db.Reservations.IgnoreQueryFilters()
                    .AnyAsync(r => r.BookId == id && r.DeletedAt == null)) return false;

            book.DeletedAt = DateTime.UtcNow;
            book.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        private static List<int> NormalizeAuthors(IEnumerable<int> ids) =>
            ids.Where(id => id > 0).Distinct().ToList();
    }
}
