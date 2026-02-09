using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ondock.api.Attributes;

/// <summary>
/// Validates that input contains only alphanumeric characters and specified allowed characters
/// </summary>
public class NoSpecialCharactersAttribute : ValidationAttribute
{
    private readonly string _allowedCharacters;
    private readonly Regex _validationRegex;

    public NoSpecialCharactersAttribute(string allowedCharacters = " -'.")
    {
        _allowedCharacters = allowedCharacters;
        var escapedChars = Regex.Escape(allowedCharacters);
        _validationRegex = new Regex($@"^[a-zA-Z0-9{escapedChars}]+$", RegexOptions.Compiled);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var input = value.ToString()!;

        if (!_validationRegex.IsMatch(input))
        {
            return new ValidationResult(
                $"The field {validationContext.DisplayName} can only contain letters, numbers, and the following characters: {_allowedCharacters}");
        }

        return ValidationResult.Success;
    }
}
