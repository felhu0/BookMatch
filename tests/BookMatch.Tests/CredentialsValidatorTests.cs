using BookMatch.Api.Models;
using BookMatch.Api.Validation;

namespace BookMatch.Tests;

// Enhetstester för CredentialsValidator: kontrollerar e-post och lösenord vid registrering och inloggning
public class CredentialsValidatorTests
{
    // Giltiga inloggningsuppgifter som utgångspunkt
    private static CredentialsRequest ValidRequest() => new("test@example.com", "en lång lösenordsfras");

    // Giltigt anrop ska godkännas
    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        var errors = CredentialsValidator.Validate(ValidRequest());

        Assert.Empty(errors);
    }

    // Tom e-post eller bara mellanslag ska nekas
    [Theory]
    [InlineData("")]     //tom
    [InlineData("  ")]   //bara mellanslag
    public void Validate_EmptyEmail_ReturnsEmailError(string email)
    {
        var request = ValidRequest() with { Email = email };

        var errors =  CredentialsValidator.Validate(request);

        Assert.True(errors.ContainsKey("email"));
    }

    // Gränsvärde: e-post med exakt maxlängd ska godkännas
    [Fact]
    public void Validate_EmailAtMaxLength_ReturnsNoErrors()
    {
        // (maxlängd - 12) tecken + "@example.com" (12 tecken) = exakt maxlängd
        var email = new string('a', CredentialsValidator.MaxEmailLength - 12) + "@example.com";
        var request = ValidRequest() with { Email = email };

        var errors = CredentialsValidator.Validate(request);

        Assert.Empty(errors);
    }

    // Gränsvärde: ett tecken över maxlängd ska nekas
    [Fact]
    public void Validate_EmailTooLong_ReturnsEmailError()
    {
        // (maxlängd - 11) tecken + "@example.com" (12 tecken) = maxlängd + 1
        var email = new string('a', CredentialsValidator.MaxEmailLength - 11) + "@example.com";
        var request = ValidRequest() with { Email = email };

        var errors = CredentialsValidator.Validate(request);

        Assert.True(errors.ContainsKey("email"));
    }

    // Tomt lösenord ska nekas
    [Fact]
    public void Validate_EmptyPassword_ReturnsPasswordError()
    {
        var request = ValidRequest() with { Password = "" };

        var errors = CredentialsValidator.Validate(request);

        Assert.True(errors.ContainsKey("password"));
    }

    // Gränsvärde: lösenord med exakt maxlängd ska godkännas
    [Fact]
    public void Validate_PasswordAtMaxLength_ReturnsNoErrors()
    {
        var request = ValidRequest() with { Password = new string('a', CredentialsValidator.MaxPasswordLength) };

        var errors = CredentialsValidator.Validate(request);

        Assert.Empty(errors);
    }

    // Gränsvärde: ett tecken över maxlängd ska nekas
    [Fact]
    public void Validate_PasswordTooLong_ReturnsPasswordError()
    {
        // Skydd mot överbelastning: hashning av mycket lång text tar tid
        var request = ValidRequest() with { Password = new string('a', CredentialsValidator.MaxPasswordLength + 1) };

        var errors = CredentialsValidator.Validate(request);

        Assert.True(errors.ContainsKey("password"));
    }

    // Saknade fält (null) ska ge fel för båda, inte krascha
    [Fact]
    public void Validate_NullEmailAndPassword_ReturnsBothErrors()
    {
        var request = new CredentialsRequest(null!, null!);

        var errors = CredentialsValidator.Validate(request);

        Assert.True(errors.ContainsKey("email"));
        Assert.True(errors.ContainsKey("password"));
    }

}
