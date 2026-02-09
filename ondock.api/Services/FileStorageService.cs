using ondock.api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ondock.api.Services;

public class FileStorageSettings
{
    public BunnyCdnSettings? BunnyCdn { get; set; }
    public LocalStorageSettings? LocalStorage { get; set; }
}

public class BunnyCdnSettings
{
    public string? StorageZoneName { get; set; }
    public string? ApiKey { get; set; }
    public string? PullZoneUrl { get; set; }
    public string? StorageEndpoint { get; set; } = "https://storage.bunnycdn.com";
}

public class LocalStorageSettings
{
    public string BasePath { get; set; } = "uploads";
    public string BaseUrl { get; set; } = "/uploads";
}

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(
        IOptions<FileStorageSettings> settings,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env,
        ILogger<FileStorageService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _env = env;
        _logger = logger;
    }

    public bool IsCdnConfigured =>
        _settings.BunnyCdn != null &&
        !string.IsNullOrEmpty(_settings.BunnyCdn.StorageZoneName) &&
        !string.IsNullOrEmpty(_settings.BunnyCdn.ApiKey) &&
        !string.IsNullOrEmpty(_settings.BunnyCdn.PullZoneUrl);

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null)
    {
        if (IsCdnConfigured)
        {
            return await UploadToBunnyCdnAsync(stream, fileName, contentType, folder);
        }
        
        return await UploadToLocalStorageAsync(stream, fileName, contentType, folder);
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (IsCdnConfigured && fileUrl.Contains(_settings.BunnyCdn!.PullZoneUrl!))
        {
            await DeleteFromBunnyCdnAsync(fileUrl);
        }
        else
        {
            DeleteFromLocalStorage(fileUrl);
        }
    }

    private async Task<string> UploadToBunnyCdnAsync(Stream stream, string fileName, string contentType, string? folder)
    {
        var cdn = _settings.BunnyCdn!;
        var uniqueFileName = GenerateUniqueFileName(fileName);
        var path = string.IsNullOrEmpty(folder) ? uniqueFileName : $"{folder}/{uniqueFileName}";

        var client = _httpClientFactory.CreateClient("BunnyCdn");
        client.DefaultRequestHeaders.Add("AccessKey", cdn.ApiKey);

        var url = $"{cdn.StorageEndpoint}/{cdn.StorageZoneName}/{path}";

        using var content = new StreamContent(stream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var response = await client.PutAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("BunnyCDN upload failed: {StatusCode} - {Error}", response.StatusCode, error);
            throw new Exception($"Failed to upload to BunnyCDN: {response.StatusCode}");
        }

        _logger.LogInformation("File uploaded to BunnyCDN: {Path}", path);
        return $"{cdn.PullZoneUrl}/{path}";
    }

    private async Task DeleteFromBunnyCdnAsync(string fileUrl)
    {
        var cdn = _settings.BunnyCdn!;
        var path = fileUrl.Replace($"{cdn.PullZoneUrl}/", "");

        var client = _httpClientFactory.CreateClient("BunnyCdn");
        client.DefaultRequestHeaders.Add("AccessKey", cdn.ApiKey);

        var url = $"{cdn.StorageEndpoint}/{cdn.StorageZoneName}/{path}";
        var response = await client.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to delete file from BunnyCDN: {Path}", path);
        }
    }

    private async Task<string> UploadToLocalStorageAsync(Stream stream, string fileName, string contentType, string? folder)
    {
        var settings = _settings.LocalStorage ?? new LocalStorageSettings();
        var uniqueFileName = GenerateUniqueFileName(fileName);
        
        var relativePath = string.IsNullOrEmpty(folder) 
            ? uniqueFileName 
            : Path.Combine(folder, uniqueFileName);

        var fullPath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, settings.BasePath, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        _logger.LogInformation("File uploaded to local storage: {Path}", relativePath);
        return $"{settings.BaseUrl}/{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
    }

    private void DeleteFromLocalStorage(string fileUrl)
    {
        var settings = _settings.LocalStorage ?? new LocalStorageSettings();
        var relativePath = fileUrl.Replace($"{settings.BaseUrl}/", "").Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, settings.BasePath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted from local storage: {Path}", relativePath);
        }
    }

    private static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        return $"{timestamp}_{guid}{extension}";
    }
}
