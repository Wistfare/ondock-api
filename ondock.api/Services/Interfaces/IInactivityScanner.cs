namespace ondock.api.Services.Interfaces;

public interface IInactivityScanner
{
    Task ScanAsync(CancellationToken ct = default);
}