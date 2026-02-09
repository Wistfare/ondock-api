namespace ondock.api.Exceptions;

public class NotFoundException : Exception
{
    public string ResourceType { get; }
    public object? ResourceId { get; }

    public NotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with identifier '{resourceId}' was not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string message) : base(message)
    {
        ResourceType = "Resource";
    }
}
