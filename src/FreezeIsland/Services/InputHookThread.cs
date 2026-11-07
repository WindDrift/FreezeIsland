using System.Runtime.InteropServices;
using FreezeIsland.Models;
using Microsoft.Extensions.Logging;

namespace FreezeIsland.Services;

/// <summary>
/// 专用线程上的低级键盘/鼠标钩子。未锁定时全部放行；锁定时按范围吞掉输入。
/// </summary>
public sealed class InputHookThread
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int HcAction = 0;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmMouseMove = 0x0200;
    private const int WmQuit = 0x0012;
    private const uint LlkhfInjected = 0x10;
    private const uint LlmhfInjected = 0x01;

    private const uint VkLControl = 0xA2;
    private const uint VkRControl = 0xA3;
    private const uint VkLMenu = 0xA4;
    private const uint VkRMenu = 0xA5;
    private const uint VkLShift = 0xA0;
    private const uint VkRShift = 0xA1;
    private const uint VkLWin = 0x5B;
    private const uint VkRWin = 0x5C;

    private readonly FreezeIslandLockService _service;
    private readonly ILogger _logger;
    private readonly ManualResetEventSlim _started = new(false);

    private Thread? _thread;
    private uint _threadId;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private HookProc? _keyboardProc;
    private HookProc? _mouseProc;
    private volatile bool _available;

    private bool _lctrl;
    private bool _rctrl;
    private bool _lalt;
    private bool _ralt;
    private bool _lshift;
    private bool _rshift;
    private bool _lwin;
    private bool _rwin;
    private bool _hotkeyDown;
    private uint _suppressKeyUp;

    private TaskCompletionSource<uint>? _captureTcs;

    public InputHookThread(FreezeIslandLockService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    public bool Available => _available;

    public bool Start()
    {
        CancelCapture();
        _thread = new Thread(ThreadProc)
        {
            IsBackground = true,
            Name = "FreezeIsland.InputHook"
        };
        _thread.Start();
        _started.Wait(TimeSpan.FromSeconds(5));
        return _available;
    }

    public void Stop()
    {
        CancelCapture();

        var thread = _thread;
        if (thread == null)
        {
            return;
        }

        if (thread.IsAlive && _threadId != 0)
        {
            PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
        }

        if (!thread.Join(TimeSpan.FromSeconds(2)))
        {
            _logger.LogWarning("FreezeIsland 输入钩子线程未在 2 秒内退出");
        }

        _thread = null;
    }

    /// <summary>
    /// 等待下一次真实键盘按键。返回 null 表示超时、取消或钩子不可用。
    /// </summary>
    public async Task<uint?> CaptureNextKeyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _captureTcs, completion, null) != null)
        {
            return null;
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
        using var registration = linkedCts.Token.Register(() => CancelCapture(completion));

        try
        {
            return await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            CancelCapture(completion);
        }
    }

    public void CancelCapture() => CancelCapture(null);

    private void CancelCapture(TaskCompletionSource<uint>? expected)
    {
        while (true)
        {
            var current = Volatile.Read(ref _captureTcs);
            if (current == null || (expected != null && !ReferenceEquals(current, expected)))
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _captureTcs, null, current) == current)
            {
                current.TrySetCanceled();
                return;
            }
        }
    }

    private bool TryCompleteCapture(uint virtualKey)
    {
        var completion = Interlocked.Exchange(ref _captureTcs, null);
        if (completion == null)
        {
            return false;
        }

        if (virtualKey == 0x1B)
        {
            completion.TrySetCanceled();
        }
        else
        {
            completion.TrySetResult(virtualKey);
        }

        return true;
    }

    private void ThreadProc()
    {
        _threadId = GetCurrentThreadId();
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
        var module = GetModuleHandle(null);

        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, module, 0);
        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, module, 0);
        _available = _keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero;
        _started.Set();

        if (!_available)
        {
            _logger.LogError(
                "安装 FreezeIsland 低级输入钩子失败（键盘 {Keyboard}，鼠标 {Mouse}，错误 {Error}）",
                _keyboardHook, _mouseHook, Marshal.GetLastWin32Error());
            UninstallHooks();
            return;
        }

        _logger.LogInformation("FreezeIsland 低级输入钩子已安装");
        try
        {
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        finally
        {
            UninstallHooks();
            CancelCapture();
            _available = false;
            _logger.LogInformation("FreezeIsland 低级输入钩子已卸载");
        }
    }

    private void UninstallHooks()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < HcAction)
        {
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
        if ((info.flags & LlkhfInjected) != 0)
        {
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        var isDown = message is WmKeyDown or WmSysKeyDown;
        var isUp = message is WmKeyUp or WmSysKeyUp;

        if (isDown && TryCompleteCapture(info.vkCode))
        {
            _suppressKeyUp = info.vkCode;
            return (IntPtr)1;
        }

        if (isUp && info.vkCode == _suppressKeyUp)
        {
            _suppressKeyUp = 0;
            return (IntPtr)1;
        }

        if (isDown || isUp)
        {
            UpdateModifierState(info.vkCode, isDown);
        }

        var hotkey = _service.CurrentHotkey;
        var isHotkeyKey = hotkey != null
                          && info.vkCode == hotkey.VirtualKey
                          && ModifiersMatch(hotkey.Modifiers);
        if (isHotkeyKey)
        {
            if (isDown)
            {
                if (!_hotkeyDown)
                {
                    _hotkeyDown = true;
                    _service.OnHotkeyPressed();
                }

                return (IntPtr)1;
            }

            if (isUp)
            {
                _hotkeyDown = false;
                return (IntPtr)1;
            }
        }

        if (_service.IsLocked && _service.Policy.ShouldBlockKey(info.vkCode, info.scanCode, info.flags))
        {
            // 锁定时吞掉键盘事件；修饰键状态由本线程独立跟踪，
            // 因此解锁热键仍然可用，且不会把修饰键状态遗留给前台程序。
            return (IntPtr)1;
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < HcAction)
        {
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        var info = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
        if ((info.flags & LlmhfInjected) != 0)
        {
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        if (!_service.IsLocked || !_service.Policy.ShouldBlockMouse(wParam.ToInt32()))
        {
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        return (IntPtr)1;
    }

    private void UpdateModifierState(uint virtualKey, bool down)
    {
        switch (virtualKey)
        {
            case VkLControl: _lctrl = down; break;
            case VkRControl: _rctrl = down; break;
            case VkLMenu: _lalt = down; break;
            case VkRMenu: _ralt = down; break;
            case VkLShift: _lshift = down; break;
            case VkRShift: _rshift = down; break;
            case VkLWin: _lwin = down; break;
            case VkRWin: _rwin = down; break;
        }
    }

    private bool ModifiersMatch(HotkeyModifiers modifiers)
    {
        var ctrl = _lctrl || _rctrl;
        var alt = _lalt || _ralt;
        var shift = _lshift || _rshift;
        var win = _lwin || _rwin;

        return ctrl == modifiers.HasFlag(HotkeyModifiers.Control)
               && alt == modifiers.HasFlag(HotkeyModifiers.Alt)
               && shift == modifiers.HasFlag(HotkeyModifiers.Shift)
               && win == modifiers.HasFlag(HotkeyModifiers.Windows);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public Point pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Point pt;
        public uint lPrivate;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
