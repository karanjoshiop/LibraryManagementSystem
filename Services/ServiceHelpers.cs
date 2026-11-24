using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    /// <summary>
    /// Small helpers shared by the feature services.
    /// </summary>
    internal static class ServiceHelpers
    {
        /// <summary>
        /// Saves and returns false on a DB constraint race instead of leaking a
        /// failed Added/Modified entry into the circuit's long-lived DbContext.
        /// </summary>
        public static async Task<bool> TrySaveAsync(this DbContext db)
        {
            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                db.DetachChanges();
                return false;
            }
        }

        /// <summary>
        /// Clears pending tracked changes after a failed save or rolled-back
        /// transaction so the context can be reused safely.
        /// </summary>
        public static void DetachChanges(this DbContext db)
        {
            foreach (var entry in db.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added
                             or EntityState.Modified
                             or EntityState.Deleted))
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
