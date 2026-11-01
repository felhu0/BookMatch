namespace BookMatch.Api.Models;

// Ett meddelande i en sparad konversation (databasentitet)
public class ConversationMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int Position { get; set; }  // ordning i konversationen 
    public ChatMessageRole Role { get; set; }
    public required string Content { get; set; }
}
