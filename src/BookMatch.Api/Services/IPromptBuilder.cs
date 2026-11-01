using BookMatch.Api.Models;
using Microsoft.Extensions.AI;

namespace BookMatch.Api.Services;

public interface IPromptBuilder
{
    //bygger meddelandelistan till AI:n med säkerhetsregler och avgränsad användartext
    List<ChatMessage> BuildMessages(ChatRequest request);
}
