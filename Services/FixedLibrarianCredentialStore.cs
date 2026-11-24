namespace LibraryManagementSystem.Services
{
    public sealed class FixedLibrarianCredentialStore : ILibrarianCredentialStore
    {
        private const string ExpectedUsername = "librarian";
        private const string ExpectedPassword = "library@123";

        public bool IsValid(string username, string password)
        {
            return string.Equals(username, ExpectedUsername, StringComparison.OrdinalIgnoreCase)
                && string.Equals(password, ExpectedPassword, StringComparison.Ordinal);
        }
    }
}
