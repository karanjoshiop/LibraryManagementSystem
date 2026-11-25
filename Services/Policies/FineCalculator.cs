namespace LibraryManagementSystem.Services.Policies
{
    /// <summary>
    /// Pure fine rules (BR-04). No database, no UI, no clock access.
    /// The service supplies the settings and the stored loan values.
    /// </summary>
    public static class FineCalculator
    {
        /// <summary>
        /// Calculates the fine for a return attempt while preserving the
        /// existing behaviour:
        /// 1. No return date means no fine.
        /// 2. A non-zero submitted fine is a manual override.
        /// 3. If the return date is unchanged and a fine already exists, keep it.
        /// 4. Otherwise use min(lateDays * finePerDay, maximumFine).
        /// </summary>
        public static decimal Calculate(
            DateOnly? returnDate,
            DateOnly dueDate,
            DateOnly? existingReturnDate,
            decimal existingFineAmount,
            decimal submittedFineAmount,
            decimal finePerDay,
            decimal maximumFine)
        {
            if (returnDate is null) return 0m;

            if (submittedFineAmount > 0m)
                return submittedFineAmount;

            if (existingReturnDate == returnDate && existingFineAmount > 0m)
                return existingFineAmount;

            var lateDays = returnDate.Value.DayNumber - dueDate.DayNumber;
            return lateDays > 0
                ? Math.Min(lateDays * finePerDay, maximumFine)
                : 0m;
        }

        /// <summary>
        /// Partial payment is valid only when it is non-negative and does not
        /// exceed the calculated fine.
        /// </summary>
        public static bool IsFinePaidValid(decimal finePaidAmount, decimal calculatedFine) =>
            finePaidAmount >= 0m && finePaidAmount <= calculatedFine;
    }
}
