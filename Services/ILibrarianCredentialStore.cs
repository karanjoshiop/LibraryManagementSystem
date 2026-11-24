namespace LibraryManagementSystem.Services
{
    public interface ILibrarianCredentialStore
    {
        bool IsValid(string username, string password);
    }
}
