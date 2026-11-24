namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record MembershipDto(
        int MembershipId,
        int MemberId,
        string MemberName,
        int MembershipTypeId,
        string TypeName,
        decimal TypeFee,
        DateOnly StartDate,
        DateOnly EndDate,
        string PaymentStatus,
        string Status);
}
