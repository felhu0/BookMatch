using BookMatch.Api.Data;
using BookMatch.Api.Models;
using BookMatch.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace BookMatch.Api.Services;

public class ConversationService(AppDbContext db) : IConversationService
{
    // Hämtar den inloggade användarens konversation
    public async Task<SavedConversationDto?> GetLatestAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversation = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var history = conversation.Messages
            .OrderBy(m => m.Position)
            .Select(m => new ChatMessageDto(m.Role, m.Content))
            .ToList();

        return new SavedConversationDto(conversation.Language, conversation.Genre, conversation.Level, history);
    }

    // Sparar konversationen: historik + ny fråga + hela svaret, ersätter den tidigare
    public async Task SaveAsync(string userId, ChatRequest request, string reply, CancellationToken cancellationToken = default)
    {
        List<ChatMessageDto> messages = [.. request.History ?? []];

        if (request.Message is not null)
        {
            messages.Add(new ChatMessageDto(ChatMessageRole.User, request.Message));
        }

        // Svaret kortas till samma maxlängd som valideringen, så att sparad historik alltid är giltig
        var maxLength = ChatRequestValidator.MaxAssistantMessageLength;
        messages.Add(new ChatMessageDto(ChatMessageRole.Assistant, reply.Length > maxLength ? reply[..maxLength] : reply));

        // Transaktion: radera gammal och spara ny konversation som en enda åtgärd
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Conversations
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        db.Conversations.Add(new Conversation
        {
            UserId = userId,
            Language = request.Language,
            Genre = request.Genre,
            Level = request.Level,
            UpdatedAt = DateTime.UtcNow,
            Messages = messages
                .Select((messages, index) => new ConversationMessage
                {
                    Position = index,
                    Role = messages.Role,
                    Content = messages.Content
                })
                .ToList()
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Raderar den inloggade användarens konversation
    public async Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        await db.Conversations
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

}
