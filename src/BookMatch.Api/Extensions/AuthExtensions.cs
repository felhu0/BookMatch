using BookMatch.Api.Data;
using BookMatch.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace BookMatch.Api.Extensions;

public static class AuthExtensions
{
    // Registrerar Identity, cookie-inloggning och behörighet i DI
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        // Cookie-inloggning med Identitys standardschema
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddAuthorization();

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = false;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager();

        // Inloggningscookie: säkerhetsflaggor och beteende för API
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "BookMatch.Auth";
            options.Cookie.HttpOnly = true;                                        // JavaScript kan inte läsa cookien
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;               // skickas bara över HTTPS 
            options.Cookie.SameSite = SameSiteMode.Strict;                         // skickas inte med anrop från andra webbplatser (skydd mot CSRF, Cross-Site Request Forgery)
            options.ExpireTimeSpan = TimeSpan.FromDays(7);                         // giltighetstiden förnyas när användaren är aktiv
            options.SlidingExpiration = true;

            // API: svara 401/403 istället för att omdirigera till en inloggningssida
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // CSRF-skydd (Cross-Site Request Forgery): token skickas i headern X-CSRF-TOKEN
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "BookMatch.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        return services;
    }
}
