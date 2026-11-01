namespace BookMatch.Api.Extensions;

// Säkerhetsheaders: instruktioner som servern skickar tillbaka i varje svar (Response headers) om vad sidan får göra i webbläsaren
public static class SecurityHeadersExtensions
{
    // CSP (Content Security Policy): regler för varifrån skript, stilar, bilder och anrop får komma
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +         // allt utan egen regel får bara komma från den egna servern
        "script-src 'self'; " +          //bara kod som ligger i filer på den egna servern.
        "style-src 'self'; " +           // bara CSS som ligger i filer på den egna servern
        "img-src 'self' data:; " +       // bara bilder från den egna servern, data: behövs för den tomma flikikonen
        "connect-src 'self'; " +         // fetch och SSE (Server-Sent Events) bara till den egna servern
        "font-src 'self'; " +            // bara typsnitt från den egna servern
        "object-src 'none'; " +          // inga plugins
        "base-uri 'self'; " +            // relativa adresser kan bara peka på den egna servern
        "form-action 'self'; " +         // formulär får bara skickas till den egna servern
        "frame-ancestors 'none'";        // sidan får inte bäddas in i andra sidor (clickjacking)

    // Middleware (kod som körs för varje anrop): lägger säkerhetsheaders på alla svar, även filerna i wwwroot
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers; // context: lägg headers på svaret

            headers.ContentSecurityPolicy = ContentSecurityPolicy;
            headers.XContentTypeOptions = "nosniff";             // webbläsaren gissar inte filtyp, t.ex. körs inte en textfil som JavaScript
            headers.XFrameOptions = "DENY";                      // ingen sida får visa BookMatch i en iframe (för äldre webbläsare)
            headers["Referrer-Policy"] = "no-referrer";          // adressen till BookMatch skickas inte vidare till andra webbplatser
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";   // kamera, mikrofon och plats är avstängda

            await next(); // next: skicka vidare
        });
}
