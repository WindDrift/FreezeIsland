using Avalonia.Media;

namespace FreezeIsland.Controls;

public static class LockVisuals
{
    public const string LockedPath =
        "M12 2a5 5 0 0 0-5 5v3H5a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2h-2V7a5 5 0 0 0-5-5zm0 2a3 3 0 0 1 3 3v3H9V7a3 3 0 0 1 3-3z";

    public const string UnlockedPath =
        "M12 2a5 5 0 0 0-5 5h2a3 3 0 1 1 6 0v3H5a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2h-2V7a5 5 0 0 0-5-5z";

    public static readonly Geometry LockedGeometry = Geometry.Parse(LockedPath);

    public static readonly Geometry UnlockedGeometry = Geometry.Parse(UnlockedPath);
}
