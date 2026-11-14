namespace FreezeIsland.Models;

public enum LockState
{
    Unlocked,
    Countdown,
    Locked
}

public enum UnlockReason
{
    Hotkey,
    Toggle,
    Automation,
    AutomationRevert,
    SecureDesktop,
    Shutdown
}

public sealed class LockStateChangedEventArgs : EventArgs
{
    public LockStateChangedEventArgs(LockState oldState, LockState newState, int countdownSeconds = 0, UnlockReason? reason = null)
    {
        OldState = oldState;
        NewState = newState;
        CountdownSeconds = countdownSeconds;
        Reason = reason;
    }

    public LockState OldState { get; }

    public LockState NewState { get; }

    public int CountdownSeconds { get; }

    public UnlockReason? Reason { get; }
}
