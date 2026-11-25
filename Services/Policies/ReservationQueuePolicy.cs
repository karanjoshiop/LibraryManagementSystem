namespace LibraryManagementSystem.Services.Policies
{
    /// <summary>
    /// Pure FCFS ordering for BR-07. ReservedAt is the primary key;
    /// ReservationId breaks ties when two rows were saved in the same tick.
    /// </summary>
    public static class ReservationQueuePolicy
    {
        public static bool ComesBefore(
            DateTime candidateReservedAt,
            long candidateReservationId,
            DateTime otherReservedAt,
            long otherReservationId)
        {
            if (candidateReservedAt != otherReservedAt)
                return candidateReservedAt < otherReservedAt;

            return candidateReservationId < otherReservationId;
        }
    }
}
