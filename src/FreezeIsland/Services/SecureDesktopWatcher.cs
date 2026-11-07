using System.Runtime.InteropServices;
using System.Text;

namespace FreezeIsland.Services;

/// <summary>
/// 判断当前输入桌面是否已切换到 Winlogon 安全桌面。
/// </summary>
public static class SecureDesktopWatcher
{
    private const uint DesktopReadObjects = 0x0001;
    private const int UoiName = 2;

    public static bool IsSecureDesktopActive()
    {
        var desktop = OpenInputDesktop(0, false, DesktopReadObjects);
        if (desktop == IntPtr.Zero)
        {
            // 打不开输入桌面时按“安全桌面”处理：宁可解锁，也不要把用户锁死。
            return true;
        }

        try
        {
            var buffer = new StringBuilder(256);
            if (!GetUserObjectInformationW(desktop, UoiName, buffer, buffer.Capacity * 2, out _))
            {
                return true;
            }

            return !string.Equals(buffer.ToString(), "Default", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CloseDesktop(desktop);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformationW(
        IntPtr hObj,
        int nIndex,
        StringBuilder pvInfo,
        int nLength,
        out int lpnLengthNeeded);
}
