using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class MemberService : IMemberService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public MemberService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<MemberDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await LoadAsync(db, null);
        }

        public async Task<int> CountAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Members.CountAsync();
        }

        public async Task<MemberDto?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var all = await LoadAsync(db, id);
            return all.FirstOrDefault();
        }

        public async Task<bool> CreateAsync(MemberInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var email = input.Email.Trim();

            // Email column is citext (case-insensitive).
            if (await db.Members.AnyAsync(m => m.Email == email))
                return false;

            // Every member starts on the free Regular plan, so they can borrow
            // immediately. If no active Regular plan is configured, fail clearly.
            var regular = await db.MembershipTypes.FirstOrDefaultAsync(
                mt => mt.Name == "Regular" && mt.IsActive);
            if (regular is null) return false;

            await using var tx = await db.Database.BeginTransactionAsync();

            var member = new Member
            {
                FullName = input.FullName.Trim(),
                Email = email,
                Phone = input.Phone?.Trim(),
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            db.Members.Add(member);

            try
            {
                await db.SaveChangesAsync(); // get the generated MemberId

                var today = DateOnly.FromDateTime(DateTime.Today);
                db.Memberships.Add(new Membership
                {
                    MemberId = member.MemberId,
                    MembershipTypeId = regular.MembershipTypeId,
                    StartDate = today,
                    EndDate = today.AddMonths(regular.DurationMonths),
                    PaymentStatus = "Paid",     // free plan is settled immediately
                    Status = input.IsActive ? "Active" : "Cancelled",
                    CreatedAt = DateTime.UtcNow
                });

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

        public async Task<bool> UpdateAsync(int id, MemberInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var member = await db.Members.FirstOrDefaultAsync(m => m.MemberId == id);
            if (member is null) return false;

            var email = input.Email.Trim();

            if (await db.Members.AnyAsync(m => m.MemberId != id && m.Email == email))
                return false;

            member.FullName = input.FullName.Trim();
            member.Email = email;
            member.Phone = input.Phone?.Trim();
            member.IsActive = input.IsActive;
            member.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var member = await db.Members.FirstOrDefaultAsync(m => m.MemberId == id);
            if (member is null) return false;

            // Check the dependents themselves, not through parent-aware filters.
            if (await db.Memberships.IgnoreQueryFilters()
                    .AnyAsync(m => m.MemberId == id && m.DeletedAt == null)) return false;
            if (await db.Loans.IgnoreQueryFilters()
                    .AnyAsync(l => l.MemberId == id && l.DeletedAt == null)) return false;
            if (await db.Reservations.IgnoreQueryFilters()
                    .AnyAsync(r => r.MemberId == id && r.DeletedAt == null)) return false;

            member.DeletedAt = DateTime.UtcNow;
            member.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        /// <summary>
        /// Loads members and decorates them with their live plan, open-loan count
        /// and outstanding fine. Several small queries are used deliberately so
        /// each one runs sequentially on this fresh context.
        /// </summary>
        private static async Task<List<MemberDto>> LoadAsync(
            LibraryDbContext db, int? memberId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = db.Members.AsNoTracking().AsQueryable();
            if (memberId is not null)
                query = query.Where(m => m.MemberId == memberId.Value);

            var members = await query
                .OrderBy(m => m.FullName)
                .Select(m => new MemberDto(
                    m.MemberId, m.FullName, m.Email, m.Phone, m.IsActive, null, null, false, 0, 0m))
                .ToListAsync();

            if (members.Count == 0) return members;

            var ids = members.Select(m => m.MemberId).ToList();

            var plans = (await db.Memberships.AsNoTracking()
                    .Where(ms => ids.Contains(ms.MemberId)
                              && ms.Status == "Active"
                              && ms.EndDate >= today)
                    .OrderByDescending(ms => ms.EndDate)
                    .Select(ms => new
                    {
                        ms.MemberId,
                        ms.MembershipType.Name,
                        ms.EndDate
                    })
                    .ToListAsync())
                .GroupBy(x => x.MemberId)
                .ToDictionary(g => g.Key, g => g.First());

            var openLoans = (await db.Loans.AsNoTracking()
                    .Where(l => ids.Contains(l.MemberId) && l.ReturnDate == null)
                    .GroupBy(l => l.MemberId)
                    .Select(g => new { MemberId = g.Key, Count = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.MemberId, x => x.Count);

            var fines = (await db.Loans.AsNoTracking()
                    .Where(l => ids.Contains(l.MemberId))
                    .GroupBy(l => l.MemberId)
                    .Select(g => new
                    {
                        MemberId = g.Key,
                        Owed = g.Sum(l => l.FineAmount - l.FinePaidAmount)
                    })
                    .ToListAsync())
                .ToDictionary(x => x.MemberId, x => x.Owed);

            return members.Select(m =>
            {
                plans.TryGetValue(m.MemberId, out var plan);
                openLoans.TryGetValue(m.MemberId, out var loans);
                fines.TryGetValue(m.MemberId, out var owed);

                return m with
                {
                    PlanName = plan?.Name,
                    MembershipEndDate = plan?.EndDate,
                    MembershipExpired = plan is null,
                    OpenLoans = loans,
                    OutstandingFine = owed
                };
            }).ToList();
        }
    }
}
