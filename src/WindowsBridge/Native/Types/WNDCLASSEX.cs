using System.Runtime.InteropServices;

namespace WindowsBridge.Native.Types;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct WNDCLASSEX
{
    public uint cbSize;
    public uint style;

    public nint lpfnWndProc;

    public int cbClsExtra;
    public int cbWndExtra;

    public nint hInstance;
    public nint hIcon;
    public nint hCursor;
    public nint hbrBackground;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string? lpszMenuName;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string lpszClassName;

    public nint hIconSm;
}