namespace FreezeIsland.Models;

/// <summary>
/// 键盘锁定的分区位掩码。
/// </summary>
[Flags]
public enum KeyboardLockScope
{
    None = 0,
    Letters = 1 << 0,
    NumberRow = 1 << 1,
    Symbols = 1 << 2,
    FunctionKeys = 1 << 3,
    Numpad = 1 << 4,
    EditAndNavigation = 1 << 5,
    ControlKeys = 1 << 6,
    Modifiers = 1 << 7,
    MediaAndSystem = 1 << 8,
    Other = 1 << 9,
    All = Letters | NumberRow | Symbols | FunctionKeys | Numpad | EditAndNavigation | ControlKeys | Modifiers | MediaAndSystem | Other
}

/// <summary>
/// 鼠标锁定的分区位掩码。指针移动由 <see cref="FreezeIslandSettings.AllowMouseMovement"/> 单独控制。
/// </summary>
[Flags]
public enum MouseLockScope
{
    None = 0,
    LeftButton = 1 << 0,
    MiddleButton = 1 << 1,
    RightButton = 1 << 2,
    SideButtons = 1 << 3,
    Wheel = 1 << 4,
    All = LeftButton | MiddleButton | RightButton | SideButtons | Wheel
}

/// <summary>
/// 将低级钩子事件归类到锁定分区。只做纯算术判断，供钩子线程直接调用。
/// </summary>
public static class InputClassifier
{
    public const uint LlkhfExtended = 0x01;
    public const uint LlkhfUp = 0x80;

    public const int WmMouseMove = 0x0200;
    public const int WmLButtonDown = 0x0201;
    public const int WmLButtonUp = 0x0202;
    public const int WmLButtonDblClk = 0x0203;
    public const int WmRButtonDown = 0x0204;
    public const int WmRButtonUp = 0x0205;
    public const int WmRButtonDblClk = 0x0206;
    public const int WmMButtonDown = 0x0207;
    public const int WmMButtonUp = 0x0208;
    public const int WmMButtonDblClk = 0x0209;
    public const int WmMouseWheel = 0x020A;
    public const int WmXButtonDown = 0x020B;
    public const int WmXButtonUp = 0x020C;
    public const int WmXButtonDblClk = 0x020D;
    public const int WmMouseHWheel = 0x020E;

    public static KeyboardLockScope ClassifyKeyboard(uint virtualKey, uint scanCode, uint flags)
    {
        var extended = (flags & LlkhfExtended) != 0;

        // 小键盘 Enter 带扩展位；NumLock 关闭时的小键盘方向键/编辑键扫描码在 0x47–0x53
        // 且不带扩展位，而主键盘上的独立方向键/编辑键带扩展位。
        if (virtualKey == 0x0D && extended)
        {
            return KeyboardLockScope.Numpad;
        }

        if (!extended && scanCode is >= 0x47 and <= 0x53)
        {
            return KeyboardLockScope.Numpad;
        }

        if (virtualKey is >= 0x60 and <= 0x6F or 0x90)
        {
            return KeyboardLockScope.Numpad;
        }

        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            return KeyboardLockScope.Letters;
        }

        if (virtualKey is >= 0x30 and <= 0x39)
        {
            return KeyboardLockScope.NumberRow;
        }

        if (virtualKey is >= 0x70 and <= 0x87)
        {
            return KeyboardLockScope.FunctionKeys;
        }

        if (virtualKey is 0x0C or 0x2D or 0x2E || virtualKey is >= 0x21 and <= 0x28)
        {
            return KeyboardLockScope.EditAndNavigation;
        }

        if (virtualKey is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C || virtualKey is >= 0xA0 and <= 0xA5)
        {
            return KeyboardLockScope.Modifiers;
        }

        if (virtualKey is 0x08 or 0x09 or 0x0D or 0x14 or 0x1B or 0x20)
        {
            return KeyboardLockScope.ControlKeys;
        }

        if (virtualKey is >= 0xBA and <= 0xC2 or >= 0xDB and <= 0xDF or 0xE1 or 0xE2)
        {
            return KeyboardLockScope.Symbols;
        }

        if (virtualKey is >= 0xA6 and <= 0xB7 or 0x2C or 0x13 or 0x91 or 0x5D or 0x5F)
        {
            return KeyboardLockScope.MediaAndSystem;
        }

        return KeyboardLockScope.Other;
    }

    public static MouseLockScope ClassifyMouse(int message) => message switch
    {
        WmLButtonDown or WmLButtonUp or WmLButtonDblClk => MouseLockScope.LeftButton,
        WmMButtonDown or WmMButtonUp or WmMButtonDblClk => MouseLockScope.MiddleButton,
        WmRButtonDown or WmRButtonUp or WmRButtonDblClk => MouseLockScope.RightButton,
        WmXButtonDown or WmXButtonUp or WmXButtonDblClk => MouseLockScope.SideButtons,
        WmMouseWheel or WmMouseHWheel => MouseLockScope.Wheel,
        _ => MouseLockScope.None
    };
}
