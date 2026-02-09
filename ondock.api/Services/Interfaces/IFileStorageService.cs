namespace ondock.api.Services.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Upload a file and return the public URL
    /// </summary>
    /// <param name="stream">File stream</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="folder">Optional folder/path prefix</param>
    /// <returns>Public URL to access the file</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null);

    /// <summary>
    /// Delete a file by its URL or path
    /// </summary>
    Task DeleteAsync(string fileUrl);

    /// <summary>
    /// Check if CDN storage is configured and available
    /// </summary>
    bool IsCdnConfigured { get; }
}
