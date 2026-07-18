using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using WindowsBridge.Native;

namespace WindowsBridge.Services;

public sealed class BridgeService(
    ILogger<BridgeService> logger,
    MqttClientService mqtt,
    NativeWindow nativeWindow)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("WindowsBridge starting.");

        await mqtt.StartAsync(stoppingToken);
        await nativeWindow.StartAsync(stoppingToken);

        logger.LogInformation("WindowsBridge started.");

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down.
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping WindowsBridge...");

        await nativeWindow.StopAsync(cancellationToken);
        await mqtt.StopAsync(cancellationToken);

        logger.LogInformation("WindowsBridge stopped.");

        await base.StopAsync(cancellationToken);
    }
}