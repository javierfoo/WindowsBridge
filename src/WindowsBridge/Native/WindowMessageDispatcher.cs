namespace WindowsBridge.Native;

public sealed class WindowMessageDispatcher
{
    private readonly IReadOnlyList<IWindowMessageHandler> _handlers;

    public WindowMessageDispatcher(
        IEnumerable<IWindowMessageHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public WindowMessageResult Dispatch(
        nint hWnd,
        uint message,
        nuint wParam,
        nint lParam)
    {
        foreach (var handler in _handlers)
        {
            WindowMessageResult result =
                handler.Handle(
                    hWnd,
                    message,
                    wParam,
                    lParam);

            if (result.Handled)
                return result;
        }

        return new WindowMessageResult(false, 0);
    }
}