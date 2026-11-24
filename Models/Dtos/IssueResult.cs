namespace LibraryManagementSystem.Models.Dtos
{
    /// <summary>
    /// Outcome of an issue/fulfil attempt, with a reason that can be shown to
    /// the librarian instead of a generic "something failed" message.
    /// </summary>
    public sealed record IssueResult(bool Success, string? ErrorMessage = null)
    {
        public static IssueResult Ok() => new(true);
        public static IssueResult Fail(string message) => new(false, message);
    }
}
