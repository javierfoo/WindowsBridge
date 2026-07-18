namespace WindowsBridge.Native.Types;

internal delegate nint WindowProc(
    nint hWnd,
    uint msg,
    nuint wParam,
    nint lParam);