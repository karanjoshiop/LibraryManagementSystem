using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using LibraryManagementSystem.Services.Policies;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class ReservationService : IReservationService
    {
        private static readonly Dictionary<string, string[]> AllowedTransitions = new()
        {
            ["Pending"] = new[] { "Fulfilled", "Cancelled", "Expired" },
            ["Fulfilled"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>(),
            ["Expired"] = Array.Empty<string>()
        };

        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public ReservationService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<ReservationDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Reservations
                .AsNoTracking()
                .OrderByDescending(r => r.ReservedAt)
                .ThenByDescending(r => r.ReservationId)
                .Select(r => new ReservationDto(
                    r.ReservationId, r.BookId, r.Book.Title,
                    r.MemberId, r.Member.FullName, r.ReservedAt, r.Status))
                .ToListAsync();
        }

        public async Task<ReservationDto?> GetByIdAsync(long id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Reservations
                .AsNoTracking()
                .Where(r => r.ReservationId == id)
                .Select(r => new ReservationDto(
                    r.ReservationId, r.BookId, r.Book.Title,
                    r.MemberId, r.Member.FullName, r.ReservedAt, r.Status))
                .FirstOrDefaultAsync();
        }

        public async Task<ReservationFormData> GetFormDataAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // A reservation is only needed when every copy is on loan. Use the
            // same availability calculation as the loan form, then keep only
            // books with zero free copies.
            var issuedByBook = (await db.Loans.IgnoreQueryFilters()
                    .Where(l => l.ReturnDate == null && l.DeletedAt == null)
                    .GroupBy(l => l.BookId)
                    .Select(g => new { BookId = g.Key, Count = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.BookId, x => x.Count);

            var books = (await db.Books
                    .AsNoTracking()
                    .OrderBy(b => b.Title)
                    .Select(b => new BookDto(
                        b.BookId, b.Title, b.ISBN, b.CategoryId,
                        b.Category.Name, b.TotalCopies,
                        new List<int>(), new List<string>(), 0))
                    .ToListAsync())
                .Select(b =>
                {
                    issuedByBook.TryGetValue(b.BookId, out var issued);
                    return b with { AvailableCopies = b.TotalCopies - issued };
                })
                .Where(b => b.AvailableCopies <= 0)
                .ToList();

            var members = await db.Members
                .AsNoTracking()
                .OrderBy(m => m.FullName)
                .Select(m => new MemberDto(
                    m.MemberId, m.FullName, m.Email, m.Phone, m.IsActive, null, null, false, 0, 0m))
                .ToListAsync();

            return new ReservationFormData(books, members);
        }

        public async Task<int> CountPendingAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Reservations.CountAsync(r => r.Status == "Pending");
        }

        /// <summary>
        /// Uses LibrarySettings.ReservationHoldDays to expire pending holds that
        /// have been waiting too long. Idempotent, safe to call on page load.
        /// </summary>
        public async Task<int> ExpireOldReservationsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var holdDays = await db.LibrarySettings
                .AsNoTracking()
                .Select(s => (int?)s.ReservationHoldDays)
                .FirstOrDefaultAsync() ?? 2;

            var cutoff = DateTime.UtcNow.AddDays(-holdDays);
            var stale = await db.Reservations
                .Where(r => r.Status == "Pending"
                         && r.ReservedAt < cutoff
                         && r.DeletedAt == null)
                .ToListAsync();

            if (stale.Count == 0) return 0;

            var now = DateTime.UtcNow;
            foreach (var reservation in stale)
            {
                reservation.Status = "Expired";
                reservation.UpdatedAt = now;
            }

            return await db.TrySaveAsync() ? stale.Count : 0;
        }

        /// <summary>
        /// BR-07: the oldest Pending reservation for a book is always served
        /// first. BR-08: the loan insert and the reservation update share one
        /// DbContext, one transaction and a single SaveChangesAsync, so there is
        /// no "loan issued but reservation not updated" half-state.
        /// </summary>
        public async Task<IssueResult> FulfillAsync(long reservationId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                // Serialize two librarians clicking Fulfil on the same row.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT \"ReservationId\" FROM \"Reservations\" WHERE \"ReservationId\" = {reservationId} AND \"DeletedAt\" IS NULL FOR UPDATE");

                var reservation = await db.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
                if (reservation is null)
                {
                    await tx.RollbackAsync();
                    return IssueResult.Fail("Reservation not found.");
                }

                if (reservation.Status != "Pending")
                {
                    await tx.RollbackAsync();
                    return IssueResult.Fail("Only a pending reservation can be fulfilled.");
                }

                // Materialize the small pending queue, then use the same pure
                // ordering rule that the unit tests cover.
                var pending = await db.Reservations
                    .AsNoTracking()
                    .Where(r => r.BookId == reservation.BookId
                             && r.Status == "Pending"
                             && r.DeletedAt == null)
                    .OrderBy(r => r.ReservedAt)
                    .ThenBy(r => r.ReservationId)
                    .Select(r => new
                    {
                        r.ReservationId,
                        r.ReservedAt,
                        MemberName = r.Member.FullName
                    })
                    .ToListAsync();

                var earlier = pending.FirstOrDefault(r =>
                    ReservationQueuePolicy.ComesBefore(
                        r.ReservedAt, r.ReservationId,
                        reservation.ReservedAt, reservation.ReservationId));

                if (earlier is not null)
                {
                    await tx.RollbackAsync();
                    return IssueResult.Fail(
                        $"Reservation #{earlier.ReservationId} for {earlier.MemberName} was made earlier and must be fulfilled first.");
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                var due = await LoanIssueHelper.GetSuggestedDueDateAsync(
                    db, reservation.MemberId, today);

                var issue = await LoanIssueHelper.AddLoanAsync(db, new LoanInput
                {
                    BookId = reservation.BookId,
                    MemberId = reservation.MemberId,
                    IssueDate = today,
                    DueDate = due
                });

                if (!issue.Success)
                {
                    db.DetachChanges();
                    await tx.RollbackAsync();
                    return issue;
                }

                reservation.Status = "Fulfilled";
                reservation.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return IssueResult.Ok();
            }
            catch (DbUpdateException)
            {
                db.DetachChanges();
                await tx.RollbackAsync();
                return IssueResult.Fail("Could not fulfil this reservation. Please try again.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IssueResult> CreateAsync(ReservationInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var book = await db.Books
                .FirstOrDefaultAsync(b => b.BookId == input.BookId);
            if (book is null)
                return IssueResult.Fail("The selected book no longer exists.");

            var member = await db.Members
                .FirstOrDefaultAsync(m => m.MemberId == input.MemberId);
            if (member is null)
                return IssueResult.Fail("The selected member no longer exists.");
            if (!member.IsActive)
                return IssueResult.Fail("This member is inactive. Reactivate the member first.");

            // Business rule: a book that still has a free copy must be borrowed,
            // not reserved. Re-check on the server so a stale page or a direct
            // POST cannot bypass the filtered dropdown.
            var issuedCopies = await db.Loans.IgnoreQueryFilters()
                .CountAsync(l => l.BookId == input.BookId
                              && l.ReturnDate == null
                              && l.DeletedAt == null);
            var available = book.TotalCopies - issuedCopies;
            if (available > 0)
                return IssueResult.Fail(
                    $"'{book.Title}' still has {available} cop{(available == 1 ? "y" : "ies")} available. Issue a loan instead of a reservation.");

            // Reservations always enter the queue as Pending.
            if (await db.Reservations.IgnoreQueryFilters()
                    .AnyAsync(r => r.BookId == input.BookId
                                && r.MemberId == input.MemberId
                                && r.Status == "Pending"
                                && r.DeletedAt == null))
                return IssueResult.Fail("This member already has a pending reservation for this book.");

            db.Reservations.Add(new Reservation
            {
                BookId = input.BookId,
                MemberId = input.MemberId,
                Status = "Pending",
                ReservedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

            return await db.TrySaveAsync()
                ? IssueResult.Ok()
                : IssueResult.Fail("Could not save the reservation. Please try again.");
        }

        public async Task<bool> UpdateAsync(long id, ReservationInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == id);
            if (entity is null) return false;

            if (!AllowedTransitions.TryGetValue(entity.Status, out var next)
                || !next.Contains(input.Status))
                return false;

            entity.Status = input.Status;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(long id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == id);
            if (entity is null) return false;

            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }
    }
}
