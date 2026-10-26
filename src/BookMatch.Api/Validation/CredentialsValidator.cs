using BookMatch.Api.Models;

namespace BookMatch.Api.Validation;

public class CredentialsValidator
{
    public const int MaxEmailLength = 256;
    public const int MaxPasswordLength = 128;

    // Returnerar valideringsfel per fält, tom lista = giltigt anrop
    public static Dictionary<string, string[]> Validate(CredentialsRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > MaxEmailLength)
        {
            errors["email"] = [$"Email is required and can be at most {MaxEmailLength} characters."];
        }

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length > MaxPasswordLength)
        {
            errors["password"] = [$"Password is required and can be at most {MaxPasswordLength} characters."];
        }

        return errors;
    }
}
