using BookMatch.Api.Data;
using BookMatch.Api.Models;
using BookMatch.Api.Services;
using BookMatch.Api.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookMatch.Tests;

// Enhetstester för ConversationService: spara, hämta och radera den senaste konversationen
public class ConversationServiceTests : IDisposable
{
    // Två användar-id för att testa att användare inte kommer åt varandras konversationer
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly List<AppDbContext> _contexts = [];

    // Körs före varje test (xUnit skapar en ny instans av klassen per test, så varje test får en egen databas)
    public ConversationServiceTests()
    {
        // SQLite i minnet (:memory:): en tom testdatabas utan fil, helt skild från appens bookmatch.db
        // Databasen finns bara så länge anslutningen är öppen och raderas i Dispose efter testet
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Inställningar som pekar databaskontexterna mot testdatabasen i minnet
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Skapar tabellerna och två användare (konversationer kräver en befintlig användare)
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        db.Users.AddRange(
            new AppUser { Id = UserA, UserName = "a@example.com" },
            new AppUser { Id = UserB, UserName = "b@example.com" });
        db.SaveChanges();
    }

    // Ny tjänst med ny databaskontext för varje operation (spara, hämta, radera), som när varje HTTP-anrop i appen får en egen kontext
    // En ny kontext har inget sparat i sitt minne (change tracker), så hämtningen måste läsa från databasen – då bevisar testet att datan verkligen sparades
    private ConversationService CreateService()
    {
        var db = new AppDbContext(_options); // ← ny databaskontext
        _contexts.Add(db);
        return new ConversationService(db);  // ← ny tjänst
    }

    // Första meddelandet från formuläret som utgångspunkt
    private static ChatRequest FirstRequest() => new("sv", Genre.Fantasy, ReadingLevel.Easy);

    // Användare utan sparad konversation ska få null (frontend visar då en tom chatt)
    [Fact]
    public async Task GetLatestAsync_NoConversation_ReturnsNull()
    {
        var result = await CreateService().GetLatestAsync(UserA);

        Assert.Null(result);
    }

    // Första svaret sparas med formulärets val, så att chatten kan återställas efter omladdning
    [Fact]
    private async Task SaveAsync_FirstMessage_SavesChoicesAndReply()
    {
        await CreateService().SaveAsync(UserA, FirstRequest(), "Svar 1");

        var result = await CreateService().GetLatestAsync(UserA);

        Assert.NotNull(result);
        Assert.Equal("sv", result.Language);
        Assert.Equal(Genre.Fantasy, result.Genre);
        Assert.Equal(ReadingLevel.Easy, result.Level);
        Assert.Equal([new ChatMessageDto(ChatMessageRole.Assistant, "Svar 1")], result.History);
    }

    // Följdfråga: historik, ny fråga och nytt svar sparas i rätt ordning (Position i databasen)
    [Fact]
    private async Task SaveAsync_FollowUp_SavesHistoryMessageAndReplyInOrder()
    {
        var request = FirstRequest() with
        {
            History =
            [
                new ChatMessageDto(ChatMessageRole.Assistant, "Svar 1"),
                new ChatMessageDto(ChatMessageRole.User, "Fråga 1"),
                new ChatMessageDto(ChatMessageRole.Assistant, "Svar 2")
            ],
            Message = "Fråga 2"
        };

        await CreateService().SaveAsync(UserA, request, "Svar 3");

        var result = await CreateService().GetLatestAsync(UserA);

        // Historik + ny fråga + nytt svar, i rätt ordning
        Assert.NotNull(result);
        Assert.Equal(
            [
                new ChatMessageDto(ChatMessageRole.Assistant, "Svar 1"),
                new ChatMessageDto(ChatMessageRole.User, "Fråga 1"),
                new ChatMessageDto(ChatMessageRole.Assistant, "Svar 2"),
                new ChatMessageDto(ChatMessageRole.User, "Fråga 2"),
                new ChatMessageDto(ChatMessageRole.Assistant, "Svar 3")
            ],
            result.History);
    }

    // Ny konversation ersätter den gamla: bara en sparad konversation per användare
    [Fact]
    public async Task SaveAsync_Twice_ReplacesPreviousConversation()
    {
        // Två sparanden: först en konversation om fantasy, sedan en ny om deckare
        await CreateService().SaveAsync(UserA, FirstRequest(), "Gammalt svar");
        await CreateService().SaveAsync(UserA, FirstRequest() with { Genre = Genre.Crime }, "Nytt svar");

        var result = await CreateService().GetLatestAsync(UserA);

        // Bara den senaste konversationen finns kvar
        Assert.NotNull(result);
        Assert.Equal(Genre.Crime, result.Genre);
        Assert.Equal("Nytt svar", result.History.Single().Content);

        // Kontroll direkt i databasen: exakt en rad för användaren (unikt index på UserId)
        await using var db = new AppDbContext(_options);
        Assert.Equal(1, await db.Conversations.CountAsync(c => c.UserId == UserA));
    }

    // För långt AI-svar kortas vid sparande, så att sparad historik klarar valideringen vid nästa följdfråga
    [Fact]
    public async Task SaveAsync_ReplyTooLong_TruncatesToMaxLength()
    {
        var longReply = new string('a', ChatRequestValidator.MaxAssistantMessageLength + 100);

        await CreateService().SaveAsync(UserA, FirstRequest(), longReply);

        var result = await CreateService().GetLatestAsync(UserA);

        // Sparad historik ska alltid klara valideringen vid nästa anrop
        Assert.NotNull(result);
        Assert.Equal(ChatRequestValidator.MaxAssistantMessageLength, result.History.Single().Content.Length);
    }

    // Behörighetskontroll: användare B kan inte läsa användare A:s konversation (skydd mot IDOR, Insecure Direct Object Reference)
    [Fact]
    public async Task GetLatestAsync_OtherUsersConversation_ReturnsNull()
    {
        await CreateService().SaveAsync(UserA, FirstRequest(), "A:s hemliga svar");

        var result = await CreateService().GetLatestAsync(UserB);

        // Användare B ser inte användare A:s konversation
        Assert.Null(result);
    }

    // Två användares konversationer hålls isär
    [Fact]
    public async Task SaveAsync_TwoUsers_KeepsConversationsSeparate()
    {
        await CreateService().SaveAsync(UserA, FirstRequest(), "Svar till A");
        await CreateService().SaveAsync(UserB, FirstRequest(), "Svar till B");

        var resultA = await CreateService().GetLatestAsync(UserA);
        var resultB = await CreateService().GetLatestAsync(UserB);

        // B:s sparande ersätter inte A:s konversation (! = vi vet att resultatet inte är null här)
        Assert.Equal("Svar till A", resultA!.History.Single().Content);
        Assert.Equal("Svar till B", resultB!.History.Single().Content);
    }

    // Radering påverkar bara den egna konversationen
    [Fact]
    public async Task DeleteAsync_OwnConversation_DeletesOnlyOwn()
    {
        await CreateService().SaveAsync(UserA, FirstRequest(), "Svar till A");
        await CreateService().SaveAsync(UserB, FirstRequest(), "Svar till B");

        await CreateService().DeleteAsync(UserA);

        // A:s konversation är borta, B:s finns kvar
        Assert.Null(await CreateService().GetLatestAsync(UserA));
        Assert.NotNull(await CreateService().GetLatestAsync(UserB));
    }

    // När konversationen raderas raderas även alla dess meddelanden (GDPR)
    [Fact]
    public async Task DeleteAsync_ConversationWithMessages_DeletesMessagesToo()
    {
        await CreateService().SaveAsync(UserA, FirstRequest(), "Svar 1");

        await CreateService().DeleteAsync(UserA);

        // Cascade delete: inga meddelanden finns kvar i databasen
        await using var db = new AppDbContext(_options);
        Assert.Equal(0, await db.Set<ConversationMessage>().CountAsync());
    }

    // Att radera när ingen konversation finns ska inte ge fel (t.ex. dubbelklick på Radera)
    [Fact]
    public async Task DeleteAsync_NoConversation_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => CreateService().DeleteAsync(UserA));

        Assert.Null(exception);
    }

    // Körs efter varje test: stänger databaskontexterna och anslutningen, då raderas databasen i minnet
    public void Dispose()
    {
        foreach (var db in _contexts)
        {
            db.Dispose();
        }

        _connection.Dispose();
    }
}
