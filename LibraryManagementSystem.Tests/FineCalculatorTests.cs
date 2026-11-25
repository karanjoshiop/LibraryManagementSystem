using LibraryManagementSystem.Services.Policies;

namespace LibraryManagementSystem.Tests
{
    public class FineCalculatorTests
    {
        private static readonly DateOnly DueDate = new(2026, 1, 1);

        [Fact]
        public void No_return_date_produces_zero_fine()
        {
            var fine = FineCalculator.Calculate(
                returnDate: null,
                dueDate: DueDate,
                existingReturnDate: null,
                existingFineAmount: 0m,
                submittedFineAmount: 0m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(0m, fine);
        }

        [Fact]
        public void Return_before_due_date_produces_zero_fine()
        {
            var fine = FineCalculator.Calculate(
                returnDate: DueDate.AddDays(-3),
                dueDate: DueDate,
                existingReturnDate: null,
                existingFineAmount: 0m,
                submittedFineAmount: 0m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(0m, fine);
        }

        [Fact]
        public void Return_on_due_date_produces_zero_fine()
        {
            var fine = FineCalculator.Calculate(
                returnDate: DueDate,
                dueDate: DueDate,
                existingReturnDate: null,
                existingFineAmount: 0m,
                submittedFineAmount: 0m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(0m, fine);
        }

        [Theory]
        [InlineData(1, 5)]
        [InlineData(5, 25)]
        [InlineData(10, 50)]
        [InlineData(40, 200)]
        [InlineData(50, 200)]
        public void Late_fine_uses_rate_then_cap(int lateDays, int expectedFine)
        {
            var fine = FineCalculator.Calculate(
                returnDate: DueDate.AddDays(lateDays),
                dueDate: DueDate,
                existingReturnDate: null,
                existingFineAmount: 0m,
                submittedFineAmount: 0m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(expectedFine, fine);
        }

        [Fact]
        public void Non_zero_submitted_fine_is_a_manual_override()
        {
            var fine = FineCalculator.Calculate(
                returnDate: DueDate.AddDays(2),
                dueDate: DueDate,
                existingReturnDate: null,
                existingFineAmount: 0m,
                submittedFineAmount: 75m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(75m, fine);
        }

        [Fact]
        public void Unchanged_return_date_keeps_existing_fine()
        {
            var returned = DueDate.AddDays(4);

            var fine = FineCalculator.Calculate(
                returnDate: returned,
                dueDate: DueDate,
                existingReturnDate: returned,
                existingFineAmount: 20m,
                submittedFineAmount: 0m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(20m, fine);
        }

        [Fact]
        public void Changed_return_date_recalculates_even_if_fine_existed()
        {
            var newReturn = DueDate.AddDays(6);

            var fine = FineCalculator.Calculate(
                returnDate: newReturn,
                dueDate: DueDate,
                existingReturnDate: DueDate.AddDays(4),
                existingFineAmount: 20m,
                submittedFineAmount: 0m,
                finePerDay: 5m,
                maximumFine: 200m);

            Assert.Equal(30m, fine);
        }

        [Fact]
        public void Fine_payment_must_be_non_negative_and_not_exceed_fine()
        {
            Assert.True(FineCalculator.IsFinePaidValid(0m, 50m));
            Assert.True(FineCalculator.IsFinePaidValid(20m, 50m));
            Assert.True(FineCalculator.IsFinePaidValid(50m, 50m));
            Assert.False(FineCalculator.IsFinePaidValid(-1m, 50m));
            Assert.False(FineCalculator.IsFinePaidValid(50.01m, 50m));
        }
    }
}
