namespace FreezeIsland.Models;

/// <summary>
/// FreezeIsland 持久化设置。
/// </summary>
public sealed class FreezeIslandSettings
{
    public string Hotkey { get; set; } = "Ctrl+Alt+F";

    public bool LockKeyboard { get; set; } = true;

    public bool LockMouse { get; set; } = true;

    public KeyboardLockScope KeyboardScope { get; set; } = KeyboardLockScope.All;

    public MouseLockScope MouseScope { get; set; } = MouseLockScope.All;

    public bool AllowMouseMovement { get; set; } = true;

    /// <summary>
    /// 开启后才启用键盘分区锁定与排除按键；关闭时锁定整个键盘且排除按键不生效。
    /// </summary>
    public bool EnableExperimentalFeatures { get; set; }

    /// <summary>
    /// 锁定期间仍然放行的键盘虚拟键码。解锁热键始终放行，无需加入此列表。
    /// </summary>
    public List<int> ExcludedKeys { get; set; } = new();

    public int CountdownSeconds { get; set; } = 5;

    public int NotificationDurationSeconds { get; set; } = 3;

    public FreezeIslandSettings Clone() => new()
    {
        Hotkey = Hotkey,
        LockKeyboard = LockKeyboard,
        LockMouse = LockMouse,
        KeyboardScope = KeyboardScope,
        MouseScope = MouseScope,
        AllowMouseMovement = AllowMouseMovement,
        EnableExperimentalFeatures = EnableExperimentalFeatures,
        ExcludedKeys = ExcludedKeys is null ? new List<int>() : new List<int>(ExcludedKeys),
        CountdownSeconds = CountdownSeconds,
        NotificationDurationSeconds = NotificationDurationSeconds
    };

    public void Normalize()
    {
        if (!HotkeyParser.TryParse(Hotkey, out var gesture) || !HotkeyParser.IsValid(gesture, out _))
        {
            Hotkey = HotkeyParser.Format(HotkeyParser.Default);
        }

        CountdownSeconds = Math.Clamp(CountdownSeconds, 0, 10);
        NotificationDurationSeconds = Math.Clamp(NotificationDurationSeconds, 1, 10);

        KeyboardScope &= KeyboardLockScope.All;
        MouseScope &= MouseLockScope.All;

        if (!LockKeyboard && !LockMouse)
        {
            LockKeyboard = true;
        }

        // 主开关开启但没有任何分区时回退为全选，避免出现“已锁定却什么都不拦截”的状态。
        // 鼠标移动单独控制，因此“只锁移动、不锁按键”仍是合法组合。
        if (LockKeyboard && KeyboardScope == KeyboardLockScope.None)
        {
            KeyboardScope = KeyboardLockScope.All;
        }

        if (LockMouse && MouseScope == MouseLockScope.None && AllowMouseMovement)
        {
            MouseScope = MouseLockScope.All;
        }

        ExcludedKeys ??= new List<int>();
        ExcludedKeys = ExcludedKeys
            .Where(virtualKey => virtualKey is > 0 and <= 0xFE)
            .Distinct()
            .OrderBy(virtualKey => virtualKey)
            .Take(64)
            .ToList();
    }

}
