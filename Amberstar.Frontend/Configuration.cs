using Amberstar.Game;

namespace Amberstar.Frontend;

public sealed class Configuration : IConfiguration
{
    const int DefaultWindowX = 10;
    const int DefaultWindowY = 10;
    const int DefaultWindowClientWidth = 1280;
    const int DefaultWindowClientHeight = 800;
    const int DefaultMonitorIndex = 0;

    const int DefaultResolution = 1;
    const int DefaultCampRegeneration = 10; // like in the original

    public Resolution[] Resolutions { get; set; } = Game.Resolution.Defaults;
    public int Resolution { get; set; } = DefaultResolution;
    public WindowMode WindowMode { get; set; } = WindowMode.Window;
    public int CampRegeneration { get; set; } = DefaultCampRegeneration;
    public int WindowX { get; set; } = DefaultWindowX;
    public int WindowY { get; set; } = DefaultWindowY;
    public int WindowClientWidth { get; set; } = DefaultWindowClientWidth;
    public int WindowClientHeight { get; set; } = DefaultWindowClientHeight;
    public int MonitorIndex { get; set; } = DefaultMonitorIndex;
    public bool Fullscreen { get; set; } = false;
    public bool ShowIntro { get; set; } = true;
    public bool ShowMainMenu { get; set; } = true;
    public GameOptions GameOptions { get; set; } = GameOptions.UnmaskedDraggedItem | GameOptions.ShowDwarfLoadingScreen; // TODO: for now we implement the original, later set this to GameOptions.Default
    public KeyMovement3D KeyMovement3D { get; set; } = KeyMovement3D.QEWASD;
    public Shortcut[] Shortcuts { get; set; } =
    [
        new(ShortcutAction.UseItem, MouseButtons.Left, KeyModifiers.Control), // Ctrl + Click
        new(ShortcutAction.EquipItem, MouseButtons.Left, KeyModifiers.Shift), // Shift + Click
    ];

    /// <summary>
    /// Loads the settings of the settings screen. Unknown or invalid values keep their defaults.
    /// </summary>
    public void Load(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var line in File.ReadAllLines(path))
        {
            var parts = line.Split('=', 2);

            if (parts.Length != 2)
                continue;

            switch (parts[0])
            {
                case nameof(Resolution):
                    if (int.TryParse(parts[1], out int resolution) && resolution >= 0 && resolution < Resolutions.Length)
                        Resolution = resolution;
                    break;
                case nameof(WindowMode):
                    if (Enum.TryParse<WindowMode>(parts[1], out var windowMode) && Enum.IsDefined(windowMode))
                        WindowMode = windowMode;
                    break;
                case nameof(CampRegeneration):
                    if (int.TryParse(parts[1], out int campRegeneration) && campRegeneration is 10 or 50)
                        CampRegeneration = campRegeneration;
                    break;
            }
        }
    }

    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllLines(path,
        [
            $"{nameof(Resolution)}={Resolution}",
            $"{nameof(WindowMode)}={WindowMode}",
            $"{nameof(CampRegeneration)}={CampRegeneration}"
        ]);
    }
}
