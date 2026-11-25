using LibraryManagementSystem.Services.Policies;

namespace LibraryManagementSystem.Tests
{
    public class BorrowRulesTests
    {
        [Theory]
        [InlineData(0, 3, false)]
        [InlineData(2, 3, false)]
        [InlineData(3, 3, true)]
        [InlineData(4, 3, true)]
        [InlineData(4, 5, false)]
        [InlineData(5, 5, true)]
        public void BR01_borrow_limit_blocks_at_the_limit(
            int activeLoans, int borrowLimit, bool expectedBlocked)
        {
            Assert.Equal(expectedBlocked,
                BorrowRules.BorrowLimitReached(activeLoans, borrowLimit));
        }

        [Theory]
        [InlineData(3, 2, 1, true)]
        [InlineData(3, 3, 0, false)]
        [InlineData(2, 2, 0, false)]
        [InlineData(0, 0, 0, false)]
        public void BR02_copy_is_available_only_when_one_is_free(
            int totalCopies, int issuedCopies, int expectedAvailable, bool expectedAvailableToBorrow)
        {
            Assert.Equal(expectedAvailable,
                BorrowRules.AvailableCopies(totalCopies, issuedCopies));
            Assert.Equal(expectedAvailableToBorrow,
                BorrowRules.IsCopyAvailable(totalCopies, issuedCopies));
        }

        [Fact]
        public void BR05_exactly_100_unpaid_fines_does_not_block()
        {
            Assert.False(BorrowRules.FinesBlock(100m, 100m));
        }

        [Fact]
        public void BR05_above_100_unpaid_fines_blocks()
        {
            Assert.True(BorrowRules.FinesBlock(100.01m, 100m));
        }

        [Fact]
        public void BR05_below_limit_does_not_block()
        {
            Assert.False(BorrowRules.FinesBlock(99.99m, 100m));
        }

        [Fact]
        public void BR05_zero_unpaid_fines_does_not_block()
        {
            Assert.False(BorrowRules.FinesBlock(0m, 100m));
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        public void BR06_duplicate_open_loan_is_detected(
            int matchingOpenLoans, bool expectedDuplicate)
        {
            Assert.Equal(expectedDuplicate,
                BorrowRules.IsDuplicateOpenLoan(matchingOpenLoans));
        }
    }
}
