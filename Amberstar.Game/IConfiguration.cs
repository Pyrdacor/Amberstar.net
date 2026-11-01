namespace Amberstar.Game;

public enum KeyMovement3D
{
    /// <summary>
    /// A and D turn instead of strafe.
    /// Left and Right as well.
    /// </summary>
    WASD,

    /// <summary>
    /// A and D strafe, Q and E turn.
    /// Left and Right same as A and D.
    /// Del and PageDown same as Q and E.
    /// </summary>
    QEWASD,
}

public enum ShortcutAction
{
    UseItem,
    EquipItem,
}

public record Shortcut(ShortcutAction Action, Key Key, KeyModifiers KeyModifiers = KeyModifiers.None)
{
    public MouseButtons MouseButtons { get; set; } = MouseButtons.None;

    public Shortcut(ShortcutAction action, MouseButtons mouseButtons, KeyModifiers keyModifiers = KeyModifiers.None)
        : this(action, Key.Invalid, keyModifiers)
    {
        MouseButtons = mouseButtons;
    }
}

// TODO: Use the stuff from the config :D
public enum WindowMode
{
    Window,
    BorderlessWindow,
    Native
}

/// <summary>
/// The size of the game window.
/// </summary>
public readonly record struct Resolution(int Width, int Height)
{
    const int VirtualWidth = 320;
    const int VirtualHeight = 200;
    const int MaxCount = 3;
    const int ReservedMonitorHeight = 100; // title bar and task bar

    public static readonly Resolution[] Defaults = [new(960, 600), new(1280, 800), new(1920, 1200)];

    /// <summary>
    /// The largest multiples of the game size which fit into a window on the given monitor, ascending.
    /// There are less of them only on very small monitors. The smallest multiple is always available.
    /// </summary>
    public static Resolution[] ForMonitor(int monitorWidth, int monitorHeight)
    {
        int maxFactor = Math.Max(1, Math.Min((monitorWidth - 1) / VirtualWidth, (monitorHeight - ReservedMonitorHeight) / VirtualHeight));
        int count = Math.Min(MaxCount, maxFactor);

        return Enumerable.Range(maxFactor - count + 1, count).Select(factor => new Resolution(factor * VirtualWidth, factor * VirtualHeight)).ToArray();
    }

    public override string ToString() => $"{Width}X{Height}";
}

public interface IConfiguration
{
    /// <summary>
    /// The window sizes which can be chosen.
    /// </summary>
    Resolution[] Resolutions { get; set; }

    /// <summary>
    /// Index into <see cref="Resolutions"/>.
    /// </summary>
    int Resolution { get; set; }

    WindowMode WindowMode { get; set; }

    /// <summary>
    /// The percentage of the maximum hit points and spell points which a rest in the camp gives.
    /// </summary>
    int CampRegeneration { get; set; }

    int WindowX { get; set; }

    int WindowY { get; set; }

    int WindowClientWidth { get; set; }

    int WindowClientHeight { get; set; }

    int MonitorIndex { get; set; }

    bool Fullscreen { get; set; }

    GameOptions GameOptions { get; set; }

    /// <summary>
    /// The intro is shown at the start (not for scenario scripts).
    /// </summary>
    bool ShowIntro { get; set; }

    /// <summary>
    /// The main menu is shown before the game (not for scenario scripts).
    /// </summary>
    bool ShowMainMenu { get; set; }

    KeyMovement3D KeyMovement3D { get; set; }

    Shortcut[] Shortcuts { get; set; }
}
