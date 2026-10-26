namespace BookMatch.Api.Models;

// Inloggningsuppgifter för registrering och inloggning
public record CredentialsRequest(string Email, string Password);
