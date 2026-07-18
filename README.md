# WindowsBridge

WindowsBridge is a lightweight Windows background service/application that bridges Windows events to Home Assistant over MQTT.

## Features

- Detect Windows shutdown
- Publish MQTT events
- Automatic MQTT reconnection
- Last Will support
- Hidden Win32 message window
- Extensible message handler architecture

## Requirements

- .NET 10 Runtime
- MQTT broker
- Home Assistant (optional)

## Configuration

Edit `appsettings.json`.

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run
```
