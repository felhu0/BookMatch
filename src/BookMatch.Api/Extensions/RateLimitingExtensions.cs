using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace BookMatch.Api.Extensions;

// Rate limiting (begränsning av antal anrop): skyddar mot missbruk, gissning av lösenord och att gratiskvoten hos Gemini tar slut
public static class RateLimitingExtensions
{
    // Policynamn (kopplas till endpoints med RequireRateLimiting)
    public const string ChatPolicy = "chat";
    public const string AccountPolicy = "account";

    // Max antal anrop per tidsfönster (15 minuter)
    private const int AnonymousChatLimit = 5; // Ej inloggade får skicka 5 chattfrågor per 15 minuter, per IP
    private const int AuthenticatedChatLimit = 20; // Inloggade får skicka 20 chattfrågor per 15 minuter, per användare
    private const int AccountLimit = 10;  // 10 försök att registrera eller logga in per 15 minuter, per IP-adress
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // Registrerar rate limiting (begränsning av antal anrop) i DI
    public static IServiceCollection AddRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Statuskod när gränsen nåtts (429 Too Many Requests)
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Chatt: inloggade per användar-id, ej inloggade per IP-adress
            options.AddPolicy(ChatPolicy, httpContext =>
            {
                // Användar-id från inloggningscookien, null om ej inloggad
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Inloggad: räknare per användare 
                // Ej inloggad: räknare per IP adress 
                //            → partitionKey    → permitLimit
                return userId is not null
                    ? CreateLimiter($"user:{userId}", AuthenticatedChatLimit)
                    : CreateLimiter($"ip:{GetIpAddress(httpContext)}", AnonymousChatLimit);
            });

            // Konto: alltid per IP adress
            options.AddPolicy(AccountPolicy, httpContext =>
                CreateLimiter($"ip:{GetIpAddress(httpContext)}", AccountLimit));

            // OnRejected körs när någon har gjort för många anrop inom 15 minuter
            options.OnRejected = async (context, cancellationToken) =>
            {
                // Frågar om rate limitern har en väntetid (Retry-After). Glidande fönster i .NET ger ingen, så TryGetMetadata returnerar false
                // och blocket hoppas över – hellre ingen väntetid än en felaktig (0 skulle betyda "försök igen direkt")
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    // Körs bara om en väntetid finns, t.ex. om en annan typ av rate limiter används i framtiden
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                // Felsvar i samma format (ProblemDetails) som resten av API:t, utan interna detaljer
                await Results.Problem(
                    detail: "Too many requests. Please try again later.",
                    statusCode: StatusCodes.Status429TooManyRequests)
                .ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }

    // Skapar en räknare med glidande fönster (sliding window)
    // sliding window: räknar anrop under de senaste 15 minuterna, i tre delar
    private static RateLimitPartition<string> CreateLimiter(string partitionKey, int permitLimit) =>
        RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit, // max antal anrop i fönstret
            Window = Window,           // fönstrets längd (15 minuter)
            SegmentsPerWindow = 3,     // antal segment (15 / 3 = 5 minuter per segment)
            QueueLimit = 0             // ingen kö: anrop över gränsen avvisas direkt med 429
        });

    // IP-adress för anropet
    private static string GetIpAddress(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
