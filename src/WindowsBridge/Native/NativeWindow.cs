using System.ComponentModel;
using System.Runtime.InteropServices;

using WindowsBridge.Native.Constants;
using WindowsBridge.Native.Interop;
using WindowsBridge.Native.Types;

namespace WindowsBridge.Native;

public sealed class NativeWindow : IAsyncDisposable
{
    private const string WindowClassName = "WindowsBridge.NativeWindow";
    private const uint PM_NOREMOVE = 0;

    private readonly WindowMessageDispatcher _dispatcher;
    private readonly WindowProc _windowProc;
    private readonly nint _hInstance;

    private Thread? _thread;
    private nint _hwnd;
    private ushort _classAtom;

    // Thread lifetime
    private readonly TaskCompletionSource _started =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _stopped =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public NativeWindow(WindowMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _windowProc = WindowProc;

        _hInstance = Kernel32.GetModuleHandle(null);

        if (_hInstance == nint.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_thread is not null)
            throw new InvalidOperationException(
                "NativeWindow has already been started.");

        _thread = new Thread(MessageThreadMain)
        {
            Name = "WindowsBridge.NativeWindow",
            IsBackground = true
        };

        _thread.Start();

        await _started.Task.WaitAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Thread? thread = _thread;
        _thread = null;

        if (thread is null)
            return;

        if (!User32.PostMessage(
                _hwnd,
                WindowMessages.WM_CLOSE,
                0,
                0))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }

        await _stopped.Task.WaitAsync(cancellationToken);

        thread.Join();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    private void MessageThreadMain()
    {
        RegisterWindowClass();

        // Ensure the thread owns a message queue.
        User32.PeekMessage(
            out _,
            nint.Zero,
            0,
            0,
            PM_NOREMOVE);

        CreateHiddenWindow();

        _started.SetResult();

        MessageLoop();

        _stopped.SetResult();
    }

    private void RegisterWindowClass()
    {
        var windowClass = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc =
                Marshal.GetFunctionPointerForDelegate(_windowProc),
            hInstance = _hInstance,
            lpszClassName = WindowClassName
        };

        _classAtom = User32.RegisterClassEx(ref windowClass);

        if (_classAtom == 0)
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
    }

    private void CreateHiddenWindow()
    {
        _hwnd = User32.CreateWindowEx(
            0,
            MakeAtom(_classAtom),
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            nint.Zero,
            nint.Zero,
            _hInstance,
            nint.Zero);

        if (_hwnd == nint.Zero)
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
    }

    private nint WindowProc(
        nint hWnd,
        uint message,
        nuint wParam,
        nint lParam)
    {
        WindowMessageResult result =
            _dispatcher.Dispatch(
                hWnd,
                message,
                wParam,
                lParam);

        if (result.Handled)
            return result.Result;

        return User32.DefWindowProc(
            hWnd,
            message,
            wParam,
            lParam);
    }

    private void MessageLoop()
    {
        for (;;)
        {
            sbyte result = User32.GetMessage(
                out MSG msg,
                nint.Zero,
                0,
                0);

            if (result == -1)
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());

            if (result == 0)
                return;

            User32.TranslateMessage(in msg);
            User32.DispatchMessage(in msg);
        }
    }

    private static nint MakeAtom(ushort atom)
    {
        return (nint)atom;
    }
}