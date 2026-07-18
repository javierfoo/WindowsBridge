using System.Runtime.InteropServices;

namespace WindowsBridge.Native.Interop;

internal static partial class Kernel32
{
    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetModuleHandleW",
        SetLastError = true)]
    internal static extern nint GetModuleHandle(
        string? lpModuleName);
}