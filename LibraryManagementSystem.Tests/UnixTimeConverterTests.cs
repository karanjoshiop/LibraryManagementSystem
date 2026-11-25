using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Tests
{
    public class UnixTimeConverterTests
    {
        private static readonly DateTime SeedTimestamp =
            new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Seed_timestamp_converts_to_known_unix_seconds()
        {
            Assert.Equal(1704067200L, UnixTime.ToSeconds(SeedTimestamp));
        }

        [Fact]
        public void Unix_seconds_convert_back_to_utc_datetime()
        {
            var value = UnixTime.FromSeconds(1704067200L);

            Assert.Equal(SeedTimestamp, value);
            Assert.Equal(DateTimeKind.Utc, value.Kind);
        }

        [Fact]
        public void Round_trip_preserves_second_precision()
        {
            var original = new DateTime(2026, 11, 25, 14, 30, 45, DateTimeKind.Utc);

            var roundTripped = UnixTime.FromSeconds(UnixTime.ToSeconds(original));

            Assert.Equal(original, roundTripped);
        }

        [Fact]
        public void Unspecified_datetime_is_treated_as_utc()
        {
            var unspecified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            Assert.Equal(1704067200L, UnixTime.ToSeconds(unspecified));
        }
    }
}
