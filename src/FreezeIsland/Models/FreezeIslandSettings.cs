namespace FreezeIsland.Models;

/// <summary>
/// FreezeIsland 持久化设置。
/// </summary>
public sealed class FreezeIslandSettings
{
    public string Hotkey { get; set; } = "Ctrl+Alt+F";

    public bool LockKeyboard { get; set; } = true;

    public bool LockMouse { get; set; } = true;

    public bool AllowMouseMovement { get; set; } = true;

    public int CountdownSeconds { get; set; } = 5;

    public int NotificationDurationSeconds { get; set; } = 3;

    public FreezeIslandSettings Clone() => (FreezeIslandSettings)MemberwiseClone();

    public void Normalize()
    {
        if (!HotkeyParser.TryParse(Hotkey, out var gesture) || !HotkeyParser.IsValid(gesture, out _))
        {
            Hotkey = HotkeyParser.Format(HotkeyParser.Default);
        }

        CountdownSeconds = Math.Clamp(CountdownSeconds, 0, 10);
        NotificationDurationSeconds = Math.Clamp(NotificationDurationSeconds, 1, 10);

        if (!LockKeyboard && !LockMouse)
        {
            LockKeyboard = true;
        }
    }
}
