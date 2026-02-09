namespace ondock.api.Configuration;

public class OAuthSettings
{
    public GoogleOAuthSettings Google { get; set; } = new();
    public AppleOAuthSettings Apple { get; set; } = new();
}

public class GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional client IDs to accept (e.g., iOS, Android)
    /// All client IDs (ClientId + AdditionalClientIds) will be validated as audiences
    /// </summary>
    public List<string> AdditionalClientIds { get; set; } = new();
    
    /// <summary>
    /// Get all valid client IDs (primary + additional)
    /// </summary>
    public IEnumerable<string> GetAllClientIds()
    {
        var clientIds = new List<string>();
        if (!string.IsNullOrEmpty(ClientId))
            clientIds.Add(ClientId);
        clientIds.AddRange(AdditionalClientIds);
        return clientIds;
    }
}

public class AppleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional client IDs to accept (e.g., iOS Bundle ID, Android package)
    /// For Apple: ClientId is typically the Service ID for web, AdditionalClientIds contains Bundle IDs
    /// </summary>
    public List<string> AdditionalClientIds { get; set; } = new();
    
    /// <summary>
    /// Get all valid client IDs/audiences (service ID + bundle IDs)
    /// </summary>
    public IEnumerable<string> GetAllClientIds()
    {
        var clientIds = new List<string>();
        if (!string.IsNullOrEmpty(ClientId))
            clientIds.Add(ClientId);
        clientIds.AddRange(AdditionalClientIds);
        return clientIds;
    }
}
