namespace FreezeIsland.Models;

/// <summary>
/// 某一时刻生效的不可变输入锁定策略。设置变化时整体替换，钩子线程只读取一次快照。
/// </summary>
public sealed class InputLockPolicy
{
    public static InputLockPolicy Default { get; } = new(
        true,
        true,
        true,
        KeyboardLockScope.All,
        MouseLockScope.All,
        Array.Empty<int>());

    private readonly int[] _excludedKeys;

    private InputLockPolicy(
        bool lockKeyboard,
        bool lockMouse,
        bool allowMouseMovement,
        KeyboardLockScope keyboardScope,
        MouseLockScope mouseScope,
        int[] excludedKeys)
    {
        LockKeyboard = lockKeyboard;
        LockMouse = lockMouse;
        AllowMouseMovement = allowMouseMovement;
        KeyboardScope = keyboardScope;
        MouseScope = mouseScope;
        _excludedKeys = excludedKeys;
    }

    public bool LockKeyboard { get; }

    public bool LockMouse { get; }

    public bool AllowMouseMovement { get; }

    public KeyboardLockScope KeyboardScope { get; }

    public MouseLockScope MouseScope { get; }

    public static InputLockPolicy Create(FreezeIslandSettings settings)
    {
        var excludedKeys = settings.ExcludedKeys is null
            ? Array.Empty<int>()
            : settings.ExcludedKeys
                .Where(virtualKey => virtualKey is > 0 and <= 0xFE)
                .Distinct()
                .OrderBy(virtualKey => virtualKey)
                .ToArray();

        return new InputLockPolicy(
            settings.LockKeyboard,
            settings.LockMouse,
            settings.AllowMouseMovement,
            settings.KeyboardScope,
            settings.MouseScope,
            excludedKeys);
    }

    public bool ShouldBlockKey(uint virtualKey, uint scanCode, uint flags)
    {
        if (!LockKeyboard || IsKeyExcluded(virtualKey))
        {
            return false;
        }

        var scope = InputClassifier.ClassifyKeyboard(virtualKey, scanCode, flags);
        return (KeyboardScope & scope) != 0;
    }

    public bool ShouldBlockMouse(int message)
    {
        if (!LockMouse)
        {
            return false;
        }

        if (message == InputClassifier.WmMouseMove)
        {
            return !AllowMouseMovement;
        }

        var scope = InputClassifier.ClassifyMouse(message);

        // 未知鼠标消息在锁定时按失败关闭处理，避免漏掉新式按键事件。
        return scope == MouseLockScope.None || (MouseScope & scope) != 0;
    }

    public bool IsKeyExcluded(uint virtualKey) => Array.BinarySearch(_excludedKeys, (int)virtualKey) >= 0;
}
