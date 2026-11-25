using LibraryManagementSystem.Services.Policies;

namespace LibraryManagementSystem.Tests
{
    public class LoanDatePolicyTests
    {
        private static readonly DateOnly IssueDate = new(2026, 1, 1);

        [Theory]
        [InlineData(14, true)]
        [InlineData(15, false)]
        public void BR03_regular_allows_exactly_14_days(int extraDays, bool expectedValid)
        {
            var dueDate = IssueDate.AddDays(extraDays);

            Assert.Equal(expectedValid,
                LoanDatePolicy.IsWithinLoanPeriod(IssueDate, dueDate, 14));
        }

        [Theory]
        [InlineData(21, true)]
        [InlineData(22, false)]
        public void BR03_premium_allows_exactly_21_days(int extraDays, bool expectedValid)
        {
            var dueDate = IssueDate.AddDays(extraDays);

            Assert.Equal(expectedValid,
                LoanDatePolicy.IsWithinLoanPeriod(IssueDate, dueDate, 21));
        }

        [Fact]
        public void Due_date_before_issue_date_is_invalid()
        {
            Assert.False(LoanDatePolicy.IsOnOrAfterIssueDate(
                IssueDate, IssueDate.AddDays(-1)));
        }

        [Fact]
        public void Due_date_equal_to_issue_date_is_valid()
        {
            Assert.True(LoanDatePolicy.IsOnOrAfterIssueDate(IssueDate, IssueDate));
        }

        [Fact]
        public void Auto_due_date_uses_14_days_when_there_is_no_plan()
        {
            var today = new DateOnly(2026, 1, 10);

            var due = LoanDatePolicy.CalculateDueDate(today, 14, null);

            Assert.Equal(new DateOnly(2026, 1, 24), due);
        }

        [Fact]
        public void Auto_due_date_uses_21_days_for_premium()
        {
            var today = new DateOnly(2026, 1, 10);
            var membershipEnd = new DateOnly(2026, 12, 31);

            var due = LoanDatePolicy.CalculateDueDate(today, 21, membershipEnd);

            Assert.Equal(new DateOnly(2026, 1, 31), due);
        }

        [Fact]
        public void Auto_due_date_is_capped_at_membership_end()
        {
            var today = new DateOnly(2026, 1, 10);
            var membershipEnd = new DateOnly(2026, 1, 15);

            var due = LoanDatePolicy.CalculateDueDate(today, 21, membershipEnd);

            Assert.Equal(membershipEnd, due);
        }

        [Fact]
        public void Auto_due_date_allows_exact_membership_end()
        {
            var today = new DateOnly(2026, 1, 10);
            var membershipEnd = new DateOnly(2026, 1, 24);

            var due = LoanDatePolicy.CalculateDueDate(today, 14, membershipEnd);

            Assert.Equal(membershipEnd, due);
        }
    }
}
