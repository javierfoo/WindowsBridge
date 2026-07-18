using WindowsBridge.Native.Constants;
using WindowsBridge.Services;

namespace WindowsBridge.Native.Handlers;

public sealed class ShutdownWindowMessageHandler(
    MqttClientService mqtt)
    : IWindowMessageHandler
{
    public WindowMessageResult Handle(
        nint hWnd,
        uint message,
        nuint wParam,
        nint lParam)
    {
        switch (message)
        {
            case WindowMessages.WM_QUERYENDSESSION:

                mqtt.NotifyShutdownAsync()
                    .GetAwaiter()
                    .GetResult();

                // Allow Windows to continue shutting down.
                return new WindowMessageResult(true, (nint)1);

            default:
                return new WindowMessageResult(false, 0);
        }
    }
}