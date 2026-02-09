namespace ondock.api.Exceptions;

public class BadRequestException : Exception
{
    public Dictionary<string, string[]>? ValidationErrors { get; }

    public BadRequestException(string message) : base(message)
    {
    }

    public BadRequestException(string message, Dictionary<string, string[]> validationErrors)
        : base(message)
    {
        ValidationErrors = validationErrors;
    }
}
