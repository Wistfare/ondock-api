using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class LoadViewExpirationScanner : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoadViewExpirationScanner> _logger;
    private readonly TimeSpan _scanInterval = TimeSpan.FromMinutes(1);

    public LoadViewExpirationScanner(
        IServiceProvider serviceProvider,
        ILogger<LoadViewExpirationScanner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LoadView Expiration Scanner started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndExpireRequestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadView Expiration Scanner");
            }

            await Task.Delay(_scanInterval, stoppingToken);
        }

        _logger.LogInformation("LoadView Expiration Scanner stopped");
    }

    private async Task ScanAndExpireRequestsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var loadViewService = scope.ServiceProvider.GetRequiredService<ILoadViewService>();

        await loadViewService.ExpireOldRequestsAsync(cancellationToken);
    }
}
