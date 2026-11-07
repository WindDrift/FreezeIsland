using Avalonia.Input;

namespace FreezeIsland.Models;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// 一个全局热键组合。
/// </summary>
public sealed record HotkeyGesture(uint VirtualKey, HotkeyModifiers Modifiers)
{
    public override string ToString() => HotkeyParser.Format(this);
}

public static class HotkeyParser
{
    public static HotkeyGesture Default { get; } = new(0x46, HotkeyModifiers.Control | HotkeyModifiers.Alt);

    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = Default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                case "MENU":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                case "META":
                    modifiers |= HotkeyModifiers.Windows;
                    break;
                default:
                    return false;
            }
        }

        if (!VirtualKeyMap.TryParse(parts[^1], out var virtualKey))
        {
            return false;
        }

        gesture = new HotkeyGesture(virtualKey, modifiers);
        return true;
    }

    public static string Format(HotkeyGesture gesture)
    {
        var parts = new List<string>(5);
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Windows)) parts.Add("Win");
        parts.Add(VirtualKeyMap.GetName(gesture.VirtualKey));
        return string.Join("+", parts);
    }

    public static bool IsValid(HotkeyGesture gesture, out string? error)
    {
        error = null;
        if (gesture.VirtualKey == 0)
        {
            error = "热键缺少主键。";
            return false;
        }

        if (VirtualKeyMap.IsModifierVirtualKey(gesture.VirtualKey))
        {
            error = "热键主键不能是修饰键。";
            return false;
        }

        if (gesture.Modifiers == HotkeyModifiers.None && !VirtualKeyMap.IsFunctionKey(gesture.VirtualKey))
        {
            error = "至少需要一个修饰键（F1–F24 除外）。";
            return false;
        }

        if (IsReserved(gesture))
        {
            error = "该组合键被 Windows 保留，无法可靠地作为锁定热键。";
            return false;
        }

        return true;
    }

    private static bool IsReserved(HotkeyGesture gesture)
    {
        // Ctrl+Alt+Del：安全注意序列，用户态无法拦截。
        if (gesture.VirtualKey == 0x2E && gesture.Modifiers == (HotkeyModifiers.Control | HotkeyModifiers.Alt))
        {
            return true;
        }

        // Ctrl+Shift+Esc：任务管理器系统快捷键。
        if (gesture.VirtualKey == 0x1B && gesture.Modifiers == (HotkeyModifiers.Control | HotkeyModifiers.Shift))
        {
            return true;
        }

        // Win+L / Alt+Tab / Alt+F4：由系统 shell 处理，低级钩子无法保证可靠拦截。
        if (gesture.VirtualKey == 0x4C && gesture.Modifiers == HotkeyModifiers.Windows)
        {
            return true;
        }

        if (gesture.VirtualKey == 0x09 && gesture.Modifiers == HotkeyModifiers.Alt)
        {
            return true;
        }

        if (gesture.VirtualKey == 0x73 && gesture.Modifiers == HotkeyModifiers.Alt)
        {
            return true;
        }

        return false;
    }
}

public static class VirtualKeyMap
{
    private static readonly Dictionary<string, uint> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TAB"] = 0x09,
        ["ENTER"] = 0x0D,
        ["RETURN"] = 0x0D,
        ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B,
        ["SPACE"] = 0x20,
        ["BACKSPACE"] = 0x08,
        ["INSERT"] = 0x2D,
        ["DELETE"] = 0x2E,
        ["HOME"] = 0x24,
        ["END"] = 0x23,
        ["PAGEUP"] = 0x21,
        ["PAGEDOWN"] = 0x22,
        ["LEFT"] = 0x25,
        ["UP"] = 0x26,
        ["RIGHT"] = 0x27,
        ["DOWN"] = 0x28
    };

    public static bool TryParse(string token, out uint virtualKey)
    {
        virtualKey = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Length == 1)
        {
            var c = char.ToUpperInvariant(token[0]);
            if (c is >= 'A' and <= 'Z')
            {
                virtualKey = c;
                return true;
            }

            if (c is >= '0' and <= '9')
            {
                virtualKey = c;
                return true;
            }
        }

        if (token[0] is 'F' or 'f' && int.TryParse(token[1..], out var f) && f is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + f - 1);
            return true;
        }

        return NamedKeys.TryGetValue(token, out virtualKey);
    }

    public static string GetName(uint virtualKey)
    {
        if (virtualKey is >= 'A' and <= 'Z') return ((char)virtualKey).ToString();
        if (virtualKey is >= '0' and <= '9') return ((char)virtualKey).ToString();
        if (virtualKey is >= 0x70 and <= 0x87) return $"F{virtualKey - 0x70 + 1}";
        foreach (var (name, vk) in NamedKeys)
        {
            if (vk == virtualKey)
            {
                return name switch
                {
                    "RETURN" => "Enter",
                    "ESC" => "Escape",
                    "BACKSPACE" => "Backspace",
                    "PAGEUP" => "PageUp",
                    "PAGEDOWN" => "PageDown",
                    _ => name[0] + name[1..].ToLowerInvariant()
                };
            }
        }

        return $"VK_{virtualKey:X2}";
    }

    public static bool IsModifierVirtualKey(uint virtualKey) => virtualKey is
        0x10 or 0x11 or 0x12 or 0x5B or 0x5C or
        0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;

    public static bool IsFunctionKey(uint virtualKey) => virtualKey is >= 0x70 and <= 0x87;

    public static bool TryFromAvaloniaKey(Key key, out uint virtualKey)
    {
        virtualKey = 0;
        if (key is >= Key.A and <= Key.Z)
        {
            virtualKey = (uint)('A' + key - Key.A);
            return true;
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            virtualKey = (uint)('0' + key - Key.D0);
            return true;
        }

        if (key is >= Key.F1 and <= Key.F24)
        {
            virtualKey = (uint)(0x70 + key - Key.F1);
            return true;
        }

        virtualKey = key switch
        {
            Key.Tab => 0x09,
            Key.Enter => 0x0D,
            Key.Escape => 0x1B,
            Key.Space => 0x20,
            Key.Back => 0x08,
            Key.Insert => 0x2D,
            Key.Delete => 0x2E,
            Key.Home => 0x24,
            Key.End => 0x23,
            Key.PageUp => 0x21,
            Key.PageDown => 0x22,
            Key.Left => 0x25,
            Key.Up => 0x26,
            Key.Right => 0x27,
            Key.Down => 0x28,
            _ => 0
        };
        return virtualKey != 0;
    }

    public static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or
        Key.LWin or Key.RWin;

    public static HotkeyModifiers ToHotkeyModifiers(KeyModifiers modifiers)
    {
        var result = HotkeyModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= HotkeyModifiers.Control;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= HotkeyModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= HotkeyModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Meta)) result |= HotkeyModifiers.Windows;
        return result;
    }

    /// <summary>
    /// 面向设置页排除按键列表的显示名，未收录的键回退到 <see cref="GetName"/>。
    /// </summary>
    public static string GetDisplayName(uint virtualKey) => virtualKey switch
    {
        0x08 => "Backspace",
        0x09 => "Tab",
        0x0C => "Clear",
        0x0D => "Enter",
        0x10 => "Shift",
        0x11 => "Ctrl",
        0x12 => "Alt",
        0x13 => "Pause",
        0x14 => "CapsLock",
        0x1B => "Esc",
        0x20 => "空格",
        0x21 => "PageUp",
        0x22 => "PageDown",
        0x23 => "End",
        0x24 => "Home",
        0x25 => "左方向键",
        0x26 => "上方向键",
        0x27 => "右方向键",
        0x28 => "下方向键",
        0x2C => "PrintScreen",
        0x2D => "Insert",
        0x2E => "Delete",
        0x5B => "左 Win",
        0x5C => "右 Win",
        0x5D => "菜单键",
        0x5F => "睡眠",
        0x6A => "小键盘 *",
        0x6B => "小键盘 +",
        0x6C => "小键盘分隔符",
        0x6D => "小键盘 -",
        0x6E => "小键盘 .",
        0x6F => "小键盘 /",
        0x90 => "NumLock",
        0x91 => "ScrollLock",
        0xA0 => "左 Shift",
        0xA1 => "右 Shift",
        0xA2 => "左 Ctrl",
        0xA3 => "右 Ctrl",
        0xA4 => "左 Alt",
        0xA5 => "右 Alt",
        0xA6 => "浏览器后退",
        0xA7 => "浏览器前进",
        0xA8 => "浏览器刷新",
        0xA9 => "浏览器停止",
        0xAA => "浏览器搜索",
        0xAB => "浏览器收藏夹",
        0xAC => "浏览器主页",
        0xAD => "静音",
        0xAE => "音量-",
        0xAF => "音量+",
        0xB0 => "下一曲",
        0xB1 => "上一曲",
        0xB2 => "停止播放",
        0xB3 => "播放/暂停",
        0xB4 => "启动邮件",
        0xB5 => "媒体选择",
        0xB6 => "启动应用 1",
        0xB7 => "启动应用 2",
        0xBA => ";",
        0xBB => "=",
        0xBC => ",",
        0xBD => "-",
        0xBE => ".",
        0xBF => "/",
        0xC0 => "`",
        0xC1 => "ABNT C1",
        0xC2 => "ABNT C2",
        0xDB => "[",
        0xDC => "\\",
        0xDD => "]",
        0xDE => "'",
        0xDF => "OEM 8",
        0xE1 => "OEM AX",
        0xE2 => "\\ (OEM 102)",
        >= 0x60 and <= 0x69 => $"小键盘 {virtualKey - 0x60}",
        _ => GetName(virtualKey)
    };
}
