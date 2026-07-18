namespace WindowsBridge.Native;

public interface IWindowMessageHandler
{
    WindowMessageResult Handle(
        nint hWnd,
        uint message,
        nuint wParam,
        nint lParam);
}