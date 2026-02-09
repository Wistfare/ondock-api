using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ondock.api.Attributes;

/// <summary>
/// Validates that input doesn't contain potential XSS or SQL injection patterns
/// </summary>
public class SanitizeInputAttribute : ValidationAttribute
{
    private static readonly Regex XssPattern = new(
        @"<script|javascript:|onerror=|onclick=|onload=|<iframe|eval\(|expression\(|vbscript:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|DECLARE)\b)|([';]--)|(/\*|\*/)|(\bOR\b.*=.*)|(\bAND\b.*=.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool AllowSqlKeywords { get; set; } = false;
    public bool AllowHtml { get; set; } = false;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var input = value.ToString()!;

        // Check for XSS patterns
        if (!AllowHtml && XssPattern.IsMatch(input))
        {
            return new ValidationResult($"The field {validationContext.DisplayName} contains invalid characters or patterns.");
        }

        // Check for SQL injection patterns
        if (!AllowSqlKeywords && SqlInjectionPattern.IsMatch(input))
        {
            return new ValidationResult($"The field {validationContext.DisplayName} contains invalid characters or patterns.");
        }

        return ValidationResult.Success;
    }
}
