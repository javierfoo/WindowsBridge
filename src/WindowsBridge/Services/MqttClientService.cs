using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MQTTnet;
using MQTTnet.Protocol;

using WindowsBridge.Mqtt;
using WindowsBridge.Options;

namespace WindowsBridge.Services;

public sealed class MqttClientService
{
    private enum ConnectionState
    {
        Stopped,
        Connecting,
        Connected,
        Reconnecting,
        Stopping
    }

    private static readonly MqttClientFactory Factory = new();
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly ILogger<MqttClientService> _logger;
    private readonly IMqttClient _client;
    private readonly MqttOptions _options;
    private readonly MqttClientOptions _clientOptions;

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _stoppingCts = new();

    private Task? _reconnectTask;
    private ConnectionState _state = ConnectionState.Stopped;

    private bool IsConnected => _client.IsConnected;

    // Pre-built MQTT messages
    private readonly MqttApplicationMessage _onlineMessage =
        new MqttApplicationMessageBuilder()
            .WithTopic(Topics.Status)
            .WithPayload(Payloads.Online)
            .WithRetainFlag(true)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

    private readonly MqttApplicationMessage _offlineMessage =
        new MqttApplicationMessageBuilder()
            .WithTopic(Topics.Status)
            .WithPayload(Payloads.Offline)
            .WithRetainFlag(true)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

    private readonly MqttApplicationMessage _shutdownMessage =
        new MqttApplicationMessageBuilder()
            .WithTopic(Topics.Shutdown)
            .WithPayload(Payloads.Shutdown)
            .WithRetainFlag(false)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

    public MqttClientService(
        ILogger<MqttClientService> logger,
        IOptions<MqttOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _client = Factory.CreateMqttClient();

        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithCredentials(_options.Username, _options.Password)
            .WithClientId(_options.ClientId)
            .WithWillTopic(Topics.Status)
            .WithWillPayload(Payloads.Offline)
            .WithWillRetain()
            .WithWillQualityOfServiceLevel(
                MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        _client.DisconnectedAsync += OnDisconnectedAsync;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_state != ConnectionState.Stopped)
            return Task.CompletedTask;

        return EnsureConnectedAsync(cancellationToken);
    }

    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        _stoppingCts.Cancel();

        if (!IsConnected || _state == ConnectionState.Stopped)
            return;

        _state = ConnectionState.Stopping;

        _logger.LogInformation(
            "Disconnecting from MQTT...");

        await PublishStatusAsync(
            false,
            CancellationToken.None);

        await _client.DisconnectAsync(
            cancellationToken: CancellationToken.None);

        _logger.LogInformation(
            "Disconnected.");

        _state = ConnectionState.Stopped;
    }

    public Task NotifyShutdownAsync() =>
        PublishShutdownAsync();

    private Task PublishAsync(
        MqttApplicationMessage message,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
            return Task.CompletedTask;

        return _client.PublishAsync(
            message,
            cancellationToken);
    }

    private Task PublishStatusAsync(
        bool online,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            online ? _onlineMessage : _offlineMessage,
            cancellationToken);

    private Task PublishShutdownAsync(
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            _shutdownMessage,
            cancellationToken);

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (IsConnected ||
                _state == ConnectionState.Stopping)
            {
                return;
            }

            _state = ConnectionState.Connecting;

            _logger.LogInformation(
                "Connecting to MQTT broker {Host}:{Port}...",
                _options.Host,
                _options.Port);

            await _client.ConnectAsync(
                _clientOptions,
                cancellationToken);

            _state = ConnectionState.Connected;

            _logger.LogInformation(
                "Connected to MQTT broker {Host}:{Port}.",
                _options.Host,
                _options.Port);

            await PublishStatusAsync(
                true,
                cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private Task OnDisconnectedAsync(
        MqttClientDisconnectedEventArgs args)
    {
        _logger.LogWarning(
            "Disconnected from MQTT broker. Reason: {Reason}",
            args.Reason);

        if (_state is ConnectionState.Stopping
            or ConnectionState.Stopped)
        {
            return Task.CompletedTask;
        }

        _state = ConnectionState.Reconnecting;

        _reconnectTask ??= ReconnectAsync();

        return Task.CompletedTask;
    }

    private async Task ReconnectAsync()
    {
        try
        {
            while (!IsConnected &&
                   _state != ConnectionState.Stopping)
            {
                try
                {
                    await EnsureConnectedAsync(
                        _stoppingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Reconnect failed. Retrying in {Delay} seconds...",
                        ReconnectDelay.TotalSeconds);
                }

                if (!IsConnected)
                {
                    await Task.Delay(
                        ReconnectDelay,
                        _stoppingCts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        finally
        {
            _reconnectTask = null;
        }
    }
}