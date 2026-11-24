using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LibraryManagementSystem.Data
{
    /// <summary>
    /// Helpers shared by the Unix-time value converters.
    /// The project convention is that every timestamp is UTC; specifying
    /// DateTimeKind.Utc here makes the conversion deterministic even when a
    /// value was constructed with an "Unspecified" kind.
    /// </summary>
    public static class UnixTime
    {
        public static long ToSeconds(DateTime value) =>
            new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
                .ToUnixTimeSeconds();

        public static DateTime FromSeconds(long seconds) =>
            DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    /// <summary>
    /// Persists a DateTime as Unix seconds (bigint) and reads it back as UTC.
    /// </summary>
    public sealed class UnixTimeConverter : ValueConverter<DateTime, long>
    {
        public UnixTimeConverter()
            : base(
                value => UnixTime.ToSeconds(value),
                value => UnixTime.FromSeconds(value))
        {
        }
    }

    /// <summary>
    /// Nullable counterpart of <see cref="UnixTimeConverter"/>.
    /// </summary>
    public sealed class NullableUnixTimeConverter : ValueConverter<DateTime?, long?>
    {
        public NullableUnixTimeConverter()
            : base(
                value => value.HasValue ? UnixTime.ToSeconds(value.Value) : (long?)null,
                value => value.HasValue ? UnixTime.FromSeconds(value.Value) : (DateTime?)null)
        {
        }
    }
}
