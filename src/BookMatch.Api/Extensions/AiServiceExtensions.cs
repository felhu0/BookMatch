using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using BookMatch.Api.Services;

namespace BookMatch.Api.Extensions;

//Här kopplas IChatClient till Gemini. Det är det enda stället i appen som vet vilken AI-leverantör som används.
public static class AiServiceExtensions
{
    // Registrerar AI-klienten (IChatClient) i DI (Dependency Injection)
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key is missing. Set 'Gemini:ApiKey' in User Secrets.");

        var model = configuration["Gemini:Model"]
            ?? throw new InvalidOperationException("Gemini model is missing. Set 'Gemini:Model' in appsettings.json.");

        var endpoint = configuration["Gemini:Endpoint"]
            ?? throw new InvalidOperationException("Gemini endpoint is missing. Set 'Gemini:Endpoint' in appsettings.json.");

        // Klient mot Geminis OpenAI-kompatibla endpoint
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        // AI-klient (IChatClient): gemensamt interface, leverantören kan bytas här utan att ändra resten av koden
        services.AddChatClient(openAiClient.GetChatClient(model).AsIChatClient());

        // Chatt-tjänst, en ny instans per HTTP-anrop
        services.AddScoped<IChatService, ChatService>();

        return services;
    }

}
