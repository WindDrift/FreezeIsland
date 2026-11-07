using Avalonia.Media;

namespace FreezeIsland.Controls;

public static class LockVisuals
{
    public const string LockedPath =
        "M12 2a5 5 0 0 0-5 5v3H5a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2h-2V7a5 5 0 0 0-5-5zm0 2a3 3 0 0 1 3 3v3H9V7a3 3 0 0 1 3-3z";

    public const string UnlockedPath =
        "M12 2a5 5 0 0 0-5 5h2a3 3 0 1 1 6 0v3H5a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2h-2V7a5 5 0 0 0-5-5z";

    public static readonly IBrush LockedBrush = new SolidColorBrush(Color.Parse("#E53935"));
    public static readonly IBrush UnlockedBrush = new SolidColorBrush(Color.Parse("#43A047"));
    public static readonly IBrush CountdownBrush = new SolidColorBrush(Color.Parse("#FB8C00"));
    public static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#9E9E9E"));
}
