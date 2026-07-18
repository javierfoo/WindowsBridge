using System.Runtime.InteropServices;

namespace WindowsBridge.Native.Types;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int x;
    public int y;
}