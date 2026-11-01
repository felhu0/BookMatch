using BookMatch.Api.Models;

namespace BookMatch.Api.Services;

//hämtar, sparar och raderar den inloggade användarens senaste konversation
public interface IConversationService
{
    Task<SavedConversationDto?> GetLatestAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveAsync(string userId, ChatRequest request, string reply, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
