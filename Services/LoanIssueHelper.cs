using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    /// <summary>
    /// Shared issue logic for the Loan form and reservation fulfilment.
    /// The caller owns the DbContext and the transaction; this helper only
    /// validates, locks the rows that matter, and adds the Loan to the context.
    /// The caller performs the single SaveChangesAsync and commit.
    /// </summary>
    internal static class LoanIssueHelper
    {
        /// <summary>
        /// Same rule as the loan form: use the member's active plan, capped at
        /// the membership end date. Falls back to 14 days when there is no plan;
        /// the issue validation will then reject the loan with a clear reason.
        /// </summary>
        public static async Task<DateOnly> GetSuggestedDueDateAsync(
            LibraryDbContext db, int memberId, DateOnly today)
        {
            var plan = await db.Memberships
                .AsNoTracking()
                .Where(m => m.MemberId == memberId
                         && m.Status == "Active"
                         && m.StartDate <= today
                         && m.EndDate >= today)
                .OrderByDescending(m => m.EndDate)
                .Select(m => new
                {
                    m.MembershipType.LoanPeriodDays,
                    m.EndDate
                })
                .FirstOrDefaultAsync();

            if (plan is null) return today.AddDays(14);

            var due = today.AddDays(plan.LoanPeriodDays);
            return plan.EndDate < due ? plan.EndDate : due;
        }

        /// <summary>
        /// Runs every borrowing rule inside the caller's transaction and stages
        /// one Loan for insert. Does NOT call SaveChangesAsync.
        /// </summary>
        public static async Task<IssueResult> AddLoanAsync(
            LibraryDbContext db, LoanInput input)
        {
            if (input.DueDate < input.IssueDate)
                return IssueResult.Fail("Due date cannot be before the issue date.");

            // PostgreSQL row locks serialize competing issues for the same copy
            // and for the same member, so the checks below cannot both pass on
            // two connections that saw the same "last available copy".
            await LockBookAsync(db, input.BookId);
            await LockMemberAsync(db, input.MemberId);

            var book = await db.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookId == input.BookId);
            if (book is null)
                return IssueResult.Fail("The selected book no longer exists.");

            var member = await db.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MemberId == input.MemberId);
            if (member is null)
                return IssueResult.Fail("The selected member no longer exists.");
            if (!member.IsActive)
                return IssueResult.Fail("This member is inactive. Reactivate or renew their membership first.");

            // Active membership covering the whole loan period.
            var membership = await db.Memberships
                .AsNoTracking()
                .Where(m => m.MemberId == input.MemberId
                         && m.Status == "Active"
                         && m.StartDate <= input.IssueDate
                         && m.EndDate >= input.DueDate)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            if (membership is null)
                return IssueResult.Fail(
                    "This member has no active membership covering the whole loan period. Renew their membership first.");

            var type = await db.MembershipTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(mt => mt.MembershipTypeId == membership.MembershipTypeId);
            if (type is null || !type.IsActive)
                return IssueResult.Fail("The member's plan is no longer active.");

            if (input.DueDate > input.IssueDate.AddDays(type.LoanPeriodDays))
                return IssueResult.Fail(
                    $"The due date exceeds the {type.LoanPeriodDays}-day loan period for the {type.Name} plan.");

            var activeLoans = await db.Loans
                .IgnoreQueryFilters()
                .Where(l => l.MemberId == input.MemberId
                         && l.ReturnDate == null
                         && l.DeletedAt == null)
                .Select(l => new { l.BookId })
                .ToListAsync();

            // BR-06: give the librarian the real reason instead of relying on
            // the unique index to produce a generic DbUpdateException.
            if (activeLoans.Any(l => l.BookId == input.BookId))
                return IssueResult.Fail(
                    $"This member already has an active loan for '{book.Title}'.");

            // BR-01: borrow limit comes from the member's plan.
            if (activeLoans.Count >= type.BorrowLimit)
                return IssueResult.Fail(
                    $"Borrow limit reached: {activeLoans.Count} of {type.BorrowLimit} books are already on loan to this member.");

            // BR-05: "above" means strictly greater than the configured limit.
            var settings = await db.LibrarySettings
                .AsNoTracking()
                .FirstOrDefaultAsync();
            var borrowingLimit = settings?.FineBorrowingLimit ?? 100m;

            var unpaidFines = await db.Loans
                .IgnoreQueryFilters()
                .Where(l => l.MemberId == input.MemberId)
                .SumAsync(l => l.FineAmount - l.FinePaidAmount);

            if (unpaidFines > borrowingLimit)
                return IssueResult.Fail(
                    $"Unpaid fines of {unpaidFines:C} exceed the {borrowingLimit:C} borrowing limit. Collect payment first.");

            // BR-02: availability is TotalCopies minus currently issued copies.
            var issuedCopies = await db.Loans
                .IgnoreQueryFilters()
                .CountAsync(l => l.BookId == input.BookId
                              && l.ReturnDate == null
                              && l.DeletedAt == null);

            if (issuedCopies >= book.TotalCopies)
                return IssueResult.Fail($"No available copies of '{book.Title}' right now.");

            db.Loans.Add(new Loan
            {
                BookId = input.BookId,
                MemberId = input.MemberId,
                IssueDate = input.IssueDate,
                DueDate = input.DueDate,
                CreatedAt = DateTime.UtcNow
            });

            return IssueResult.Ok();
        }

        private static Task<int> LockBookAsync(LibraryDbContext db, int bookId) =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT \"BookId\" FROM \"Books\" WHERE \"BookId\" = {bookId} AND \"DeletedAt\" IS NULL FOR UPDATE");

        private static Task<int> LockMemberAsync(LibraryDbContext db, int memberId) =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT \"MemberId\" FROM \"Members\" WHERE \"MemberId\" = {memberId} AND \"DeletedAt\" IS NULL FOR UPDATE");
    }
}
