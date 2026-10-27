using BookMatch.Api.Endpoints;
using BookMatch.Api.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// AI-tjänster (Gemini via IChatClient)
builder.Services.AddAiServices(builder.Configuration);

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddAuth();

// Rate limiting (begränsning av antal anrop) i två nivåer
builder.Services.AddRateLimits();

// Enum som text i JSON (JavaScript Object Notation), siffror tillåts inte
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false));

    // Alla fält i anropet måste finnas, inget fylls i med standardvärden
    options.SerializerOptions.RespectRequiredConstructorParameters = true;

    // Null tillåts inte i fält som inte är markerade som nullable (t.ex. string?)
    options.SerializerOptions.RespectNullableAnnotations = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// Frontend: visar index.html, filer i wwwroot serveras
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Rate limiting: efter inloggning, så att inloggade känns igen och får sin egen gräns
app.UseRateLimiter();

app.MapAntiforgeryEndpoints();
app.MapChatEndpoints();
app.MapAccountEndpoints();

app.Run();

