using BookMatch.Api.Models;

namespace BookMatch.Api.Services;

// Chatt-tjänst: skickar formulärets val till AI:n och returnerar svaret
public interface IChatService
{
    IAsyncEnumerable<string> StreamReplyAsync(ChatRequest request, CancellationToken concellationToken = default);
}
