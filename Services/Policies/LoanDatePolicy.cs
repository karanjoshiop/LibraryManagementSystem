namespace LibraryManagementSystem.Services.Policies
{
    /// <summary>
    /// Pure loan-date rules used by BR-03 and the auto due-date feature.
    /// </summary>
    public static class LoanDatePolicy
    {
        /// <summary>
        /// The due date may not be earlier than the issue date.
        /// </summary>
        public static bool IsOnOrAfterIssueDate(DateOnly issueDate, DateOnly dueDate) =>
            dueDate >= issueDate;

        /// <summary>
        /// BR-03: Regular allows 14 days, Premium allows 21 days.
        /// The exact last day is valid; one day more is rejected.
        /// </summary>
        public static bool IsWithinLoanPeriod(
            DateOnly issueDate, DateOnly dueDate, int loanPeriodDays) =>
            dueDate <= issueDate.AddDays(loanPeriodDays);

        /// <summary>
        /// Same rule as the loan form: add the plan period to today, but never
        /// go beyond the membership end date. With no plan, fall back to 14 days.
        /// </summary>
        public static DateOnly CalculateDueDate(
            DateOnly today, int loanPeriodDays, DateOnly? membershipEndDate)
        {
            var due = today.AddDays(loanPeriodDays);
            return membershipEndDate is { } end && end < due ? end : due;
        }
    }
}
