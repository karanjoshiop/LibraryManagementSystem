using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class MembershipService : IMembershipService
    {
        private static readonly string[] ValidPaymentStatuses = { "Paid", "Pending" };
        private static readonly string[] ValidStatuses = { "Active", "Expired", "Cancelled" };

        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public MembershipService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<MembershipDto>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Memberships
                .AsNoTracking()
                .OrderByDescending(m => m.StartDate)
                .Select(m => new MembershipDto(
                    m.MembershipId, m.MemberId, m.Member.FullName,
                    m.MembershipTypeId, m.MembershipType.Name, m.MembershipType.Fee,
                    m.StartDate, m.EndDate, m.PaymentStatus, m.Status))
                .ToListAsync();
        }

        public async Task<MembershipDto?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Memberships
                .AsNoTracking()
                .Where(m => m.MembershipId == id)
                .Select(m => new MembershipDto(
                    m.MembershipId, m.MemberId, m.Member.FullName,
                    m.MembershipTypeId, m.MembershipType.Name, m.MembershipType.Fee,
                    m.StartDate, m.EndDate, m.PaymentStatus, m.Status))
                .FirstOrDefaultAsync();
        }

        public async Task<MembershipFormData> GetFormDataAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var members = await db.Members
                .AsNoTracking()
                .OrderBy(m => m.FullName)
                .Select(m => new MemberDto(
                    m.MemberId, m.FullName, m.Email, m.Phone, m.IsActive, null, null, false, 0, 0m))
                .ToListAsync();

            var types = await db.MembershipTypes
                .AsNoTracking()
                .OrderBy(mt => mt.Name)
                .Select(mt => new MembershipTypeDto(
                    mt.MembershipTypeId, mt.Name, mt.DurationMonths,
                    mt.Fee, mt.BorrowLimit, mt.LoanPeriodDays, mt.IsActive))
                .ToListAsync();

            return new MembershipFormData(members, types);
        }

        public async Task<int> CountActiveAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await db.Memberships
                .CountAsync(m => m.Status == "Active" && m.EndDate >= today);
        }

        public async Task<bool> CreateAsync(MembershipInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            NormalizeStatusForDates(input);
            await NormalizePaymentStatusAsync(db, input);
            if (!await IsValidAsync(db, input, null)) return false;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var start = input.StartDate == default ? today : input.StartDate;

            db.Memberships.Add(new Membership
            {
                MemberId = input.MemberId,
                MembershipTypeId = input.MembershipTypeId,
                StartDate = start,
                EndDate = input.EndDate,
                PaymentStatus = input.PaymentStatus,
                Status = input.Status,
                CreatedAt = DateTime.UtcNow
            });

            return await db.TrySaveAsync();
        }

        public async Task<bool> UpdateAsync(int id, MembershipInput input)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == id);
            if (entity is null) return false;

            NormalizeStatusForDates(input);
            await NormalizePaymentStatusAsync(db, input);
            if (!await IsValidAsync(db, input, id)) return false;

            entity.MemberId = input.MemberId;
            entity.MembershipTypeId = input.MembershipTypeId;
            entity.StartDate = input.StartDate;
            entity.EndDate = input.EndDate;
            entity.PaymentStatus = input.PaymentStatus;
            entity.Status = input.Status;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == id);
            if (entity is null) return false;

            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        /// <summary>
        /// Moves a member from their current plan to another active plan in one
        /// transaction. The old row is expired (history is kept) and a fresh
        /// Active row is created from today.
        /// </summary>
        public async Task<bool> UpgradeAsync(int memberId, int newMembershipTypeId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var current = await db.Memberships.FirstOrDefaultAsync(
                m => m.MemberId == memberId && m.Status == "Active" && m.DeletedAt == null);
            var nextType = await db.MembershipTypes.FirstOrDefaultAsync(
                mt => mt.MembershipTypeId == newMembershipTypeId && mt.IsActive);
            var member = await db.Members.FirstOrDefaultAsync(m => m.MemberId == memberId);
            var currentType = current is null ? null : await db.MembershipTypes.FirstOrDefaultAsync(
                mt => mt.MembershipTypeId == current.MembershipTypeId && mt.IsActive);

            if (current is null || nextType is null || member is null || currentType is null
                || current.MembershipTypeId == newMembershipTypeId
                || nextType.Fee <= currentType.Fee)
                return false;

            current.Status = "Expired";
            current.UpdatedAt = DateTime.UtcNow;

            db.Memberships.Add(new Membership
            {
                MemberId = memberId,
                MembershipTypeId = nextType.MembershipTypeId,
                StartDate = today,
                EndDate = today.AddMonths(nextType.DurationMonths),
                PaymentStatus = nextType.Fee == 0 ? "Paid" : "Pending",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });

            member.IsActive = true;
            member.UpdatedAt = DateTime.UtcNow;

            try
            {
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

        /// <summary>
        /// Re-activates an expired/cancelled membership as a new row starting
        /// today, so the old term stays in the history. Free plans are Paid.
        /// </summary>
        public async Task<bool> RenewAsync(int membershipId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var old = await db.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);
            if (old is null) return false;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var stillLive = old.Status == "Active" && old.EndDate >= today;
            if (stillLive) return false;

            var type = await db.MembershipTypes.FirstOrDefaultAsync(
                mt => mt.MembershipTypeId == old.MembershipTypeId && mt.IsActive);
            var member = await db.Members.FirstOrDefaultAsync(m => m.MemberId == old.MemberId);
            if (type is null || member is null) return false;

            if (await db.Memberships.AnyAsync(
                    m => m.MemberId == old.MemberId
                      && m.MembershipId != membershipId
                      && m.Status == "Active"
                      && m.EndDate >= today
                      && m.DeletedAt == null))
                return false;

            db.Memberships.Add(new Membership
            {
                MemberId = old.MemberId,
                MembershipTypeId = old.MembershipTypeId,
                StartDate = today,
                EndDate = today.AddMonths(type.DurationMonths),
                PaymentStatus = type.Fee == 0 ? "Paid" : "Pending",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });

            member.IsActive = true;
            member.UpdatedAt = DateTime.UtcNow;

            return await db.TrySaveAsync();
        }

        /// <summary>
        /// Idempotent maintenance: marks ended Active memberships as Expired and
        /// deactivates members who no longer have a live plan. Safe to call on
        /// every page load.
        /// </summary>
        public async Task<int> RefreshExpiriesAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            var expired = await db.Memberships
                .Where(m => m.Status == "Active" && m.EndDate < today && m.DeletedAt == null)
                .ToListAsync();
            if (expired.Count == 0) return 0;

            var memberIds = expired.Select(m => m.MemberId).ToHashSet();
            var now = DateTime.UtcNow;

            foreach (var membership in expired)
            {
                membership.Status = "Expired";
                membership.UpdatedAt = now;
            }

            var liveMemberIds = await db.Memberships
                .Where(m => memberIds.Contains(m.MemberId)
                         && m.Status == "Active"
                         && m.EndDate >= today
                         && m.DeletedAt == null)
                .Select(m => m.MemberId)
                .ToListAsync();
            var liveSet = liveMemberIds.ToHashSet();

            var deactivate = await db.Members
                .Where(m => memberIds.Contains(m.MemberId)
                         && !liveSet.Contains(m.MemberId)
                         && m.IsActive
                         && m.DeletedAt == null)
                .ToListAsync();

            foreach (var member in deactivate)
            {
                member.IsActive = false;
                member.UpdatedAt = now;
            }

            return await db.TrySaveAsync() ? expired.Count : 0;
        }

        /// <summary>
        /// A plan whose end date has passed cannot be Active, even if the form
        /// still had "Active" selected. Save it as Expired instead so the list,
        /// borrowing checks and renew button all agree immediately.
        /// </summary>
        private static void NormalizeStatusForDates(MembershipInput input)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (input.Status == "Active" && input.EndDate < today)
                input.Status = "Expired";
        }

        private static async Task NormalizePaymentStatusAsync(
            LibraryDbContext db, MembershipInput input)
        {
            var fee = await db.MembershipTypes
                .Where(mt => mt.MembershipTypeId == input.MembershipTypeId)
                .Select(mt => (decimal?)mt.Fee)
                .FirstOrDefaultAsync();

            // A free plan is never "Pending" - it is settled the moment it is given.
            if (fee == 0m) input.PaymentStatus = "Paid";
        }

        private async Task<bool> IsValidAsync(
            LibraryDbContext db, MembershipInput input, int? currentId)
        {
            if (input.EndDate < input.StartDate) return false;
            if (!ValidPaymentStatuses.Contains(input.PaymentStatus)) return false;
            if (!ValidStatuses.Contains(input.Status)) return false;

            var member = await db.Members
                .FirstOrDefaultAsync(m => m.MemberId == input.MemberId);
            var type = await db.MembershipTypes
                .FirstOrDefaultAsync(mt => mt.MembershipTypeId == input.MembershipTypeId);

            if (member is null || type is null) return false;

            // An inactive member/plan cannot hold an Active membership.
            if (input.Status == "Active" && (!member.IsActive || !type.IsActive)) return false;

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Belt-and-braces: an Active row may never end in the past.
            if (input.Status == "Active" && input.EndDate < today) return false;

            // "Expired" must actually be in the past.
            if (input.Status == "Expired" && input.EndDate > today) return false;

            // A paid plan cannot be recorded as settled for free by accident.
            if (type.Fee == 0 && input.PaymentStatus != "Paid") return false;

            // At most one live Active membership per member.
            if (input.Status == "Active")
            {
                var query = db.Memberships.IgnoreQueryFilters()
                    .Where(m => m.MemberId == input.MemberId
                             && m.Status == "Active"
                             && m.EndDate >= today
                             && m.DeletedAt == null);

                if (currentId is not null)
                    query = query.Where(m => m.MembershipId != currentId.Value);

                if (await query.AnyAsync()) return false;
            }

            return true;
        }
    }
}
