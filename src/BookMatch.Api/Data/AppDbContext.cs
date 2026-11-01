using BookMatch.Api.Models;
using BookMatch.Api.Validation;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookMatch.Api.Data;

//Identity-tabeller och sparade konversationer
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Conversation>(conversation =>
        {
            conversation.HasIndex(c => c.UserId).IsUnique();

            // Raderas användaren raderas konversationen automatiskt
            conversation.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            conversation.Property(c => c.Language).HasMaxLength(ChatRequestValidator.LanguageCodeLength);
            conversation.Property(c => c.Genre).HasConversion<string>().HasMaxLength(50);
            conversation.Property(c => c.Level).HasConversion<string>().HasMaxLength(50);

            // Raderas konversationen raderas dess meddelanden
            conversation.HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConversationMessage>(message =>
        {
            message.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            message.Property(m => m.Content).HasMaxLength(ChatRequestValidator.MaxAssistantMessageLength);
        });
    }
}
