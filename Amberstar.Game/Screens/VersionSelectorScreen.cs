using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The versions of the game data which can be played (e.g. English Amiga).
/// <paramref name="Selected"/> is called with the index of the chosen version.
/// </summary>
public sealed record VersionSelection(IReadOnlyList<string> Names, int CurrentIndex, Action<int> Selected);

/// <summary>
/// Not part of the original. It is shown at the start if there are several game versions.
/// The game was created with the data of the current version, so the choice of another
/// version creates a new game.
/// </summary>
internal sealed class VersionSelectorScreen() : WindowScreen(64, 0, 12, 0, WindowDisplayLayer)
{
	const byte BackgroundDisplayLayer = 100;
	const byte WindowDisplayLayer = 110;
	const byte ControlDisplayLayer = 120;
	const int TitleHeight = 12;
	const int LineHeight = 10;
	const int TextColorIndex = 15;
	const int HoveredTextColorIndex = 1;
	readonly List<(Label Label, Rect Area)> entries = [];
	IColoredRect? background;
	int hoveredIndex = -1;

	public override ScreenType Type { get; } = ScreenType.VersionSelector;

	public override bool CloseOnEscape { get; } = false;

	public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;

	VersionSelection Selection => Game.VersionSelection!;

	public override int HeightInTiles => 2 + (TitleHeight + Selection.Names.Count * LineHeight + 15) / 16;

	public override int Y => (200 - HeightInTiles * 16) / 2;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		// the layout and the portraits are hidden
		background = Game.CreateColoredRect(Layer.UI, new(0, 0), new(Game.VirtualScreenWidth, Game.VirtualScreenHeight), Color.Black);

		if (background != null)
			background.DisplayLayer = BackgroundDisplayLayer;

		Game.Cursor.CursorType = CursorType.Sword;

		byte palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];
		var client = ClientArea;
		var title = AddLabel(0, 0, client.Size.Width, TitleHeight, ControlDisplayLayer);
		title.SetText("Game version", TextColorIndex, TextManager.TransparentPaper, palette);
		title.Visible = true;

		entries.Clear();

		for (int i = 0; i < Selection.Names.Count; i++)
		{
			int y = TitleHeight + i * LineHeight;
			var label = AddLabel(8, y + 1, client.Size.Width - 8, LineHeight - 1, ControlDisplayLayer);
			entries.Add((label, new Rect(client.Position.X, client.Position.Y + y, client.Size.Width, LineHeight)));
		}

		hoveredIndex = Selection.CurrentIndex;
		UpdateEntries();
	}

	public override void Close()
	{
		Game.Destroy(background);
		background = null;

		base.Close();
	}

	void UpdateEntries()
	{
		byte palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

		for (int i = 0; i < entries.Count; i++)
		{
			int color = i == hoveredIndex ? HoveredTextColorIndex : TextColorIndex;
			entries[i].Label.SetText(Selection.Names[i], color, TextManager.TransparentPaper, palette);
			entries[i].Label.Visible = true;
		}
	}

	void Hover(int index)
	{
		if (index != hoveredIndex)
		{
			hoveredIndex = index;
			UpdateEntries();
		}
	}

	internal void Select(int index)
	{
		if (index == Selection.CurrentIndex)
			Game.VersionSelected();
		else
			Selection.Selected(index);
	}

	int EntryAt(Position position) => entries.FindIndex(entry => entry.Area.Contains(position));

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		int index = EntryAt(position);

		if (index != -1)
			Hover(index);

		base.MouseMove(position, buttons);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left)
		{
			int index = EntryAt(position);

			if (index != -1)
			{
				Select(index);
				return true;
			}
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		switch (key)
		{
			case Key.Up:
				Hover((hoveredIndex + entries.Count - 1) % entries.Count);
				return true;
			case Key.Down:
				Hover((hoveredIndex + 1) % entries.Count);
				return true;
			case Key.Enter:
				Select(hoveredIndex);
				return true;
		}

		return base.KeyDown(key, keyModifiers);
	}
}
