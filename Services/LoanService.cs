using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class LoanService : ILoanService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public LoanService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<LoanDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await db.Loans
                .AsNoTracking()
                .OrderByDescending(l => l.IssueDate)
                .ThenByDescending(l => l.LoanId)
                .Select(l => new LoanDto(
                    l.LoanId, l.BookId, l.Book.Title,
                    l.MemberId, l.Member.FullName,
                    l.IssueDate, l.DueDate, l.ReturnDate,
                    l.FineAmount, l.FinePaidAmount,
                    l.ReturnDate != null ? "Returned"
                        : l.DueDate < today ? "Overdue" : "On loan"))
                .ToListAsync();
        }

        public async Task<LoanDto?> GetByIdAsync(long id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await db.Loans
                .AsNoTracking()
                .Where(l => l.LoanId == id)
                .Select(l => new LoanDto(
                    l.LoanId, l.BookId, l.Book.Title,
                    l.MemberId, l.Member.FullName,
                    l.IssueDate, l.DueDate, l.ReturnDate,
                    l.FineAmount, l.FinePaidAmount,
                    l.ReturnDate != null ? "Returned"
                        : l.DueDate < today ? "Overdue" : "On loan"))
                .FirstOrDefaultAsync();
        }

        public async Task<LoanFormData> GetFormDataAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Only books with a free copy can be issued. Count open loans per
            // book first, then project the form data with real availability.
            var issuedByBook = (await db.Loans
                    .AsNoTracking()
                    .Where(l => l.ReturnDate == null)
                    .GroupBy(l => l.BookId)
                    .Select(g => new { BookId = g.Key, Count = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.BookId, x => x.Count);

            var books = await db.Books
                .AsNoTracking()
                .OrderBy(b => b.Title)
                .Select(b => new BookDto(
                    b.BookId, b.Title, b.ISBN, b.CategoryId,
                    b.Category.Name, b.TotalCopies,
                    new List<int>(), new List<string>(), b.TotalCopies))
                .ToListAsync();

            books = books.Select(b =>
            {
                issuedByBook.TryGetValue(b.BookId, out var issued);
                return b with { AvailableCopies = b.TotalCopies - issued };
            }).ToList();

            var members = await db.Members
                .AsNoTracking()
                .OrderBy(m => m.FullName)
                .Select(m => new MemberDto(
                    m.MemberId, m.FullName, m.Email, m.Phone, m.IsActive, null, null, false, 0, 0m))
                .ToListAsync();

            return new LoanFormData(books, members);
        }

        public async Task<BorrowInfo?> GetBorrowInfoAsync(int memberId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            var member = await db.Members
                .AsNoTracking()
                .AnyAsync(m => m.MemberId == memberId);
            if (!member) return null;

            var plan = await db.Memberships
                .AsNoTracking()
                .Where(m => m.MemberId == memberId
                         && m.Status == "Active"
                         && m.StartDate <= today
                         && m.EndDate >= today)
                .OrderByDescending(m => m.EndDate)
                .Select(m => new
                {
                    m.MembershipType.Name,
                    m.MembershipType.LoanPeriodDays,
                    m.MembershipType.BorrowLimit,
                    m.EndDate
                })
                .FirstOrDefaultAsync();

            var openLoans = await db.Loans
                .CountAsync(l => l.MemberId == memberId && l.ReturnDate == null);

            if (plan is null)
                return new BorrowInfo(null, 14, 0, openLoans, null, false);

            return new BorrowInfo(
                plan.Name, plan.LoanPeriodDays, plan.BorrowLimit,
                openLoans, plan.EndDate, true);
        }

        public async Task<int> CountOnLoanAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Loans.CountAsync(l => l.ReturnDate == null);
        }

        public async Task<int> CountOverdueAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await db.Loans
                .CountAsync(l => l.ReturnDate == null && l.DueDate < today);
        }

        /// <summary>
        /// BR-08: all validation reads, the availability check and the insert
        /// run in one explicit transaction. Row locks in LoanIssueHelper stop
        /// two issues from claiming the same last copy or exceeding a limit.
        /// </summary>
        public async Task<IssueResult> CreateAsync(LoanInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var result = await LoanIssueHelper.AddLoanAsync(db, input);
                if (!result.Success)
                {
                    db.DetachChanges();
                    await tx.RollbackAsync();
                    return result;
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return IssueResult.Ok();
            }
            catch (DbUpdateException)
            {
                db.DetachChanges();
                await tx.RollbackAsync();
                return IssueResult.Fail("Could not save the loan. Please try again.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// BR-08: the loan row is locked, the fine is calculated and the return
        /// is written in one explicit transaction. ReturnDate, FineAmount and
        /// FinePaidAmount therefore always commit together.
        /// </summary>
        public async Task<bool> UpdateAsync(long id, LoanInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                // Serialize concurrent returns of the same loan.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT \"LoanId\" FROM \"Loans\" WHERE \"LoanId\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE");

                var loan = await db.Loans.FirstOrDefaultAsync(l => l.LoanId == id);
                if (loan is null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // A returned loan cannot be silently re-opened: it could violate the
                // unique index for an active loan on the same book/member pair.
                if (loan.ReturnDate is not null && input.ReturnDate is null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (input.ReturnDate is not null && input.ReturnDate < loan.IssueDate)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (input.FinePaidAmount < 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var settings = await db.LibrarySettings.FirstOrDefaultAsync();
                var finePerDay = settings?.FinePerDay ?? 5m;
                var maximumFine = settings?.MaximumFine ?? 200m;

                decimal fine;
                if (input.ReturnDate is null)
                {
                    fine = 0;
                }
                else if (input.FineAmount > 0)
                {
                    fine = input.FineAmount; // manual override from the edit form
                }
                else if (loan.ReturnDate == input.ReturnDate && loan.FineAmount > 0)
                {
                    fine = loan.FineAmount; // return date unchanged; keep existing fine
                }
                else
                {
                    var lateDays = input.ReturnDate.Value.DayNumber - loan.DueDate.DayNumber;
                    fine = lateDays > 0 ? Math.Min(lateDays * finePerDay, maximumFine) : 0;
                }

                if (input.FinePaidAmount > fine)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                loan.ReturnDate = input.ReturnDate;
                loan.FineAmount = fine;
                loan.FinePaidAmount = input.FinePaidAmount;
                loan.UpdatedAt = DateTime.UtcNow;

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
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var loan = await db.Loans.FirstOrDefaultAsync(l => l.LoanId == id);
            if (loan is null) return false;

            loan.DeletedAt = DateTime.UtcNow;
            loan.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }
    }
}
