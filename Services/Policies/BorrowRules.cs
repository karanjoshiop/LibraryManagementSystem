namespace LibraryManagementSystem.Services.Policies
{
    /// <summary>
    /// Pure borrowing decisions used by BR-01, BR-02, BR-05 and BR-06.
    /// The service still owns the database reads; this class only decides.
    /// </summary>
    public static class BorrowRules
    {
        /// <summary>
        /// BR-01: the member is blocked only after reaching the plan limit.
        /// </summary>
        public static bool BorrowLimitReached(int activeLoans, int borrowLimit) =>
            activeLoans >= borrowLimit;

        /// <summary>
        /// BR-02: copies left after open loans are counted.
        /// </summary>
        public static int AvailableCopies(int totalCopies, int issuedCopies) =>
            totalCopies - issuedCopies;

        /// <summary>
        /// BR-02: a book can be issued only when at least one copy is free.
        /// </summary>
        public static bool IsCopyAvailable(int totalCopies, int issuedCopies) =>
            AvailableCopies(totalCopies, issuedCopies) > 0;

        /// <summary>
        /// BR-05: "above ₹100" means strictly greater than the limit.
        /// Exactly ₹100 still allows borrowing.
        /// </summary>
        public static bool FinesBlock(decimal unpaidFines, decimal borrowingLimit) =>
            unpaidFines > borrowingLimit;

        /// <summary>
        /// BR-06: any positive number of matching open loans is a duplicate.
        /// </summary>
        public static bool IsDuplicateOpenLoan(int matchingOpenLoans) =>
            matchingOpenLoans > 0;
    }
}
