using LibraryManagementSystem.Components;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- Authentication / authorization ---
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationCore();
builder.Services.AddHttpContextAccessor();

// Custom provider: reads the cookie during prerender, persists it into the
// interactive circuit so refreshes keep the user signed in.
builder.Services.AddScoped<LibrarianAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<LibrarianAuthenticationStateProvider>());

// Fixed demo credentials (used by the /account/login form endpoint).
builder.Services.AddScoped<ILibrarianCredentialStore, FixedLibrarianCredentialStore>();

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IMembershipTypeService, MembershipTypeService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found. " +
        "Set it in user-secrets for local development.");

// Services use a factory so each operation gets a fresh context (safe in Blazor
// circuits). The scoped registration is kept for EF tooling/migrations.
builder.Services.AddDbContextFactory<LibraryDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// --- HTTP request pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Required so the auth cookie is read into HttpContext.User on every request.
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Signed-in librarians should never see the login page (even via direct URL).
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/login", StringComparison.OrdinalIgnoreCase)
        && context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/");
        return;
    }
    await next(context);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- Auth endpoints ---
// A plain HTML form POST is a real browser request, so the cookie is written
// to the browser's response (unlike an HttpClient call from inside a circuit).
app.MapPost("/account/login", async (
    HttpContext context,
    ILibrarianCredentialStore credentials) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString().Trim(); // trim only username
    var password = form["password"].ToString();        // password exact match
    // Sanitize on receipt so an open redirect can never be echoed back to the form.
    var requestedUrl = form["returnUrl"].ToString();
    var returnUrl = IsLocalUrl(requestedUrl) ? requestedUrl : string.Empty;

    if (!credentials.IsValid(username, password))
    {
        var errorUrl = returnUrl.Length == 0
            ? "/login?error=1"
            : $"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(errorUrl);
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, username),
        new(ClaimTypes.Role, "Librarian")
    };

    var identity = new ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme);

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Redirect(IsLocalUrl(returnUrl) ? returnUrl : "/");
});

app.MapPost("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// --- Demo JSON endpoint ---
app.MapGet("/api/categories", async (ICategoryService categories) =>
    Results.Ok(await categories.GetAllAsync()));

app.Run();

static bool IsLocalUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url)) return false;

    url = url.Trim();

    if (url.StartsWith("//") || url.StartsWith("/\\")) return false;
    if (url.Contains('\r') || url.Contains('\n')) return false;
    if (Uri.TryCreate(url, UriKind.Absolute, out _)) return false;

    return url.StartsWith('/');
}
