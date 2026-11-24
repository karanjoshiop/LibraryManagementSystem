using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LibraryManagementSystem.Services
{
    public sealed class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Supplies Blazor with the signed-in librarian.
    /// - Static SSR (after a form POST redirect or F5): the cookie auth
    ///   middleware has already populated HttpContext.User, so we use it.
    /// - Interactive circuit: HttpContext is gone, so we restore the user
    ///   that Routes.razor persisted during prerendering.
    /// </summary>
    public sealed class LibrarianAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PersistentComponentState _persistentState;

        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public LibrarianAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            PersistentComponentState persistentState)
        {
            _httpContextAccessor = httpContextAccessor;
            _persistentState = persistentState;

            // 1) Static prerender: cookie -> HttpContext.User.
            var httpUser = _httpContextAccessor.HttpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
            {
                _currentUser = httpUser;
                return;
            }

            // 2) Interactive circuit: restore the prerendered user.
            if (_persistentState.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var info)
                && info is not null)
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, info.Username),
                        new Claim(ClaimTypes.Role, info.Role)
                    },
                    authenticationType: CookieAuthenticationDefaults.AuthenticationScheme));
            }
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_currentUser));

        /// <summary>
        /// Called by Routes.razor at the end of prerendering so the interactive
        /// circuit can restore the same user.
        /// </summary>
        public void PersistCurrentUser()
        {
            if (_currentUser.Identity?.IsAuthenticated != true)
                return;

            _persistentState.PersistAsJson(nameof(UserInfo), new UserInfo
            {
                Username = _currentUser.Identity.Name ?? string.Empty,
                Role = _currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "Librarian"
            });
        }
    }
}
