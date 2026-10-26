using BookMatch.Api.Models;
using BookMatch.Api.Validation;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BookMatch.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account")
            .AddEndpointFilter<AntiforgeryFilter>();

        // Registrera nytt konto och logga in direkt
        group.MapPost("/register", async (
            CredentialsRequest request,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager) =>
        {
            var errors = CredentialsValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var user = new AppUser { UserName = request.Email, Email = request.Email };
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                // Avslöja inte att e-posten redan finns (skydd mot kartläggning av konton)
                if (result.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
                {
                    return Results.Problem(
                        detail: "Registration could not be completed.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                // Lösenords- och e-postregler får visas, de hjälper användaren
                return Results.ValidationProblem(result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Ok(new { email = user.Email });
        });

        // Logga in, lockout räknar felaktiga försök
        group.MapPost("/login", async (CredentialsRequest request, SignInManager<AppUser> signInManager) =>
        {
            var errors = CredentialsValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var result = await signInManager.PasswordSignInAsync(
                request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return Results.Problem(
                    detail: "Too many failed attempts. Try again later.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (!result.Succeeded)
            {
                // Samma meddelande oavsett om e-post eller lösenord är fel
                return Results.Problem(
                    detail: "Too many failed attempts. Try again later.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new { eamil = request.Email });
        });

        group.MapPost("/logout", async (SignInManager<AppUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // Vem är inloggad (401 om ingen)
        group.MapGet("/me", (ClaimsPrincipal user) =>
            Results.Ok(new { email = user.FindFirstValue(ClaimTypes.Name) })
        ).RequireAuthorization();

        return app;
    }
}
