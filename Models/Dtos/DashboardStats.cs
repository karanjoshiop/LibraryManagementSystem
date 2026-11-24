namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record DashboardStats(
        int Books,
        int Categories,
        int Members,
        int Authors,
        int ActiveMemberships,
        int BooksOnLoan,
        int OverdueLoans,
        int PendingReservations);
}
