using System.Runtime.InteropServices;
using WindowsBridge.Native.Types;

namespace WindowsBridge.Native.Interop;

internal static class User32
{
    [DllImport(
        "user32.dll",
        EntryPoint = "DefWindowProcW",
        SetLastError = true)]
    internal static extern nint DefWindowProc(
        nint hWnd,
        uint msg,
        nuint wParam,
        nint lParam);

    [DllImport(
        "user32.dll",
        EntryPoint = "RegisterClassExW",
        SetLastError = true)]
    internal static extern ushort RegisterClassEx(
        ref WNDCLASSEX windowClass);

    [DllImport(
        "user32.dll",
        EntryPoint = "CreateWindowExW",
        SetLastError = true)]
    internal static extern nint CreateWindowEx(
        uint dwExStyle,
        nint lpClassName,
        string lpWindowName,
        uint dwStyle,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern sbyte GetMessage(
        out MSG lpMsg,
        nint hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax);

    [DllImport("user32.dll")]
    internal static extern bool TranslateMessage(
        in MSG lpMsg);

    [DllImport("user32.dll")]
    internal static extern nint DispatchMessage(
        in MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool PeekMessage(
        out MSG lpMsg,
        nint hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax,
        uint wRemoveMsg);

    [DllImport(
        "user32.dll",
        EntryPoint = "PostMessageW",
        SetLastError = true)]
    internal static extern bool PostMessage(
        nint hWnd,
        uint Msg,
        nuint wParam,
        nint lParam);

    [DllImport(
        "user32.dll",
        ExactSpelling = true)]
    internal static extern void PostQuitMessage(
        int nExitCode);
}