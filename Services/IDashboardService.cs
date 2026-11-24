using LibraryManagementSystem.Models.Dtos;

namespace LibraryManagementSystem.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetStatsAsync();
    }
}
