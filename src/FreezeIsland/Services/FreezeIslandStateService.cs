using System.ComponentModel;
using System.Runtime.CompilerServices;
using FreezeIsland.Models;

namespace FreezeIsland.Services;

/// <summary>
/// 供 UI、提醒和自动化读取的锁定状态镜像。状态变更应在 UI 线程触发。
/// </summary>
public sealed class FreezeIslandStateService : INotifyPropertyChanged
{
    private LockState _state = LockState.Unlocked;
    private int _countdownRemaining;
    private string _hotkeyText = HotkeyParser.Format(HotkeyParser.Default);
    private bool _hookAvailable;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<LockStateChangedEventArgs>? StateChanged;

    public LockState State => _state;

    public bool IsLocked => _state == LockState.Locked;

    public int CountdownRemaining => _countdownRemaining;

    public string HotkeyText => _hotkeyText;

    public bool HookAvailable => _hookAvailable;

    public void SetState(LockState state, int countdownSeconds = 0, UnlockReason? reason = null)
    {
        var oldState = _state;
        if (oldState == state && countdownSeconds == _countdownRemaining)
        {
            return;
        }

        _state = state;
        _countdownRemaining = countdownSeconds;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(CountdownRemaining));

        try
        {
            StateChanged?.Invoke(this, new LockStateChangedEventArgs(oldState, state, countdownSeconds, reason));
        }
        catch
        {
            // 提醒/订阅者异常不应阻断状态机。
        }
    }

    public void SetCountdown(int seconds)
    {
        if (_countdownRemaining == seconds)
        {
            return;
        }

        _countdownRemaining = seconds;
        OnPropertyChanged(nameof(CountdownRemaining));
    }

    public void SetHotkey(string hotkey)
    {
        if (_hotkeyText == hotkey)
        {
            return;
        }

        _hotkeyText = hotkey;
        OnPropertyChanged(nameof(HotkeyText));
    }

    public void SetHookAvailable(bool available)
    {
        if (_hookAvailable == available)
        {
            return;
        }

        _hookAvailable = available;
        OnPropertyChanged(nameof(HookAvailable));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
