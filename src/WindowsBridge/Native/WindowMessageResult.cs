namespace WindowsBridge.Native;

public readonly record struct WindowMessageResult(
    bool Handled,
    nint Result);