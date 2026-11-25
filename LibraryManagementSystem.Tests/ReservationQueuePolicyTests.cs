using LibraryManagementSystem.Services.Policies;

namespace LibraryManagementSystem.Tests
{
    public class ReservationQueuePolicyTests
    {
        private static readonly DateTime Earlier =
            new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime Later =
            new(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);

        [Fact]
        public void BR07_earlier_reserved_at_wins()
        {
            Assert.True(ReservationQueuePolicy.ComesBefore(
                Earlier, 10, Later, 5));
        }

        [Fact]
        public void BR07_later_reserved_at_does_not_win()
        {
            Assert.False(ReservationQueuePolicy.ComesBefore(
                Later, 5, Earlier, 10));
        }

        [Fact]
        public void BR07_same_timestamp_lower_id_wins()
        {
            Assert.True(ReservationQueuePolicy.ComesBefore(
                Earlier, 5, Earlier, 10));
        }

        [Fact]
        public void BR07_same_timestamp_higher_id_does_not_win()
        {
            Assert.False(ReservationQueuePolicy.ComesBefore(
                Earlier, 10, Earlier, 5));
        }

        [Fact]
        public void Same_reservation_does_not_come_before_itself()
        {
            Assert.False(ReservationQueuePolicy.ComesBefore(
                Earlier, 7, Earlier, 7));
        }
    }
}
