using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<LibraryDbContext> _dbFactory;

        public DashboardService(IDbContextFactory<LibraryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<DashboardStats> GetStatsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Sequential awaits on the same fresh context: no concurrency issue.
            return new DashboardStats(
                await db.Books.CountAsync(),
                await db.Categories.CountAsync(),
                await db.Members.CountAsync(),
                await db.Authors.CountAsync(),
                await db.Memberships.CountAsync(m => m.Status == "Active" && m.EndDate >= today),
                await db.Loans.CountAsync(l => l.ReturnDate == null),
                await db.Loans.CountAsync(l => l.ReturnDate == null && l.DueDate < today),
                await db.Reservations.CountAsync(r => r.Status == "Pending"));
        }
    }
}
