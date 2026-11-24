namespace LibraryManagementSystem.Models.Dtos
{
    public sealed record MembershipFormData(
        List<MemberDto> Members,
        List<MembershipTypeDto> Types);
}
