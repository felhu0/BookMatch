using BookMatch.Api.Models;

namespace BookMatch.Api.Services;

// Chatt-tjänst: skickar formulärets val till AI:n och returnerar svaret
public interface IChatService
{
    Task<string> GetReplyAsync(ChatRequest request, CancellationToken concellationToken = default);
}
