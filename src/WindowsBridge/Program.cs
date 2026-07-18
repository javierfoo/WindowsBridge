using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using WindowsBridge.Native;
using WindowsBridge.Native.Handlers;
using WindowsBridge.Options;
using WindowsBridge.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Services.Configure<MqttOptions>(
    builder.Configuration.GetSection(MqttOptions.SectionName));

builder.Services.AddSingleton<MqttClientService>();

builder.Services.AddSingleton<NativeWindow>();
builder.Services.AddSingleton<WindowMessageDispatcher>();

builder.Services.AddSingleton<IWindowMessageHandler,
    ShutdownWindowMessageHandler>();

builder.Services.AddHostedService<BridgeService>();

await builder.Build().RunAsync();