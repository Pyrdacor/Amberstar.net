using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Not part of the original. It is shown after the intro and before the game.
/// </summary>
internal sealed class MainMenuScreen() : WindowScreen(64, WindowY, 12, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowHeightInTiles = 5;
	const int WindowY = 60;
	const byte BackgroundDisplayLayer = 100;
	const byte TitleDisplayLayer = 101;
	const byte WindowDisplayLayer = 110;
	const byte ControlDisplayLayer = 120;
	const int LineHeight = 12;
	const int TextColorIndex = 15;
	const int HoveredTextColorIndex = 1;
	const int DisabledTextColorIndex = 3;
	const int ContinueIndex = 0;
	const int NewGameIndex = 1;
	const int LoadGameIndex = 2;
	const int QuitIndex = 3;
	static readonly string[] EnglishTexts = ["CONTINUE GAME", "START NEW GAME", "LOAD GAME", "QUIT"];
	static readonly string[] GermanTexts = ["SPIEL FORTSETZEN", "NEUES SPIEL STARTEN", "SPIEL LADEN", "BEENDEN"];
	readonly List<(Label Label, Rect Area)> entries = [];
	readonly bool[] enabled = new bool[4];
	IColoredRect? background;
	int hoveredIndex = NewGameIndex;
	bool checkSavegames = false;

	public override ScreenType Type { get; } = ScreenType.MainMenu;

	public override bool CloseOnEscape { get; } = false;

	public override bool CloseOnRightClick { get; } = false;

	public override bool AllowInventoryAccess => false;

	public override bool AllowCharacterSelection => false;

	public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;

	byte Palette => Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

	internal string ScriptEnabledEntries => string.Concat(enabled.Select(entry => entry ? '1' : '0'));

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		// the layout and the portraits are hidden
		background = Game.CreateColoredRect(Layer.UI, new(0, 0), new(Game.VirtualScreenWidth, Game.VirtualScreenHeight), Color.Black);

		if (background != null)
			background.DisplayLayer = BackgroundDisplayLayer;

		Game.HidePartyMemberNames();
		Game.Cursor.CursorType = CursorType.Sword;

		// the controls are placed relative to the client area of the window
		var client = ClientArea;
		var title = AddImage(-client.Position.X, -client.Position.Y, Game.VirtualScreenWidth, 32, Game.GraphicIndexProvider.GetCharacterCreatorTitleIndex(), TitleDisplayLayer, true);
		title.PaletteIndex = Game.PaletteIndexProvider.GetCharacterCreatorTitlePaletteIndex();

		entries.Clear();

		for (int i = 0; i < enabled.Length; i++)
		{
			int y = i * LineHeight;
			var label = AddLabel(0, y + 2, client.Size.Width, LineHeight - 2, ControlDisplayLayer);
			label.Alignment = TextAlignment.Center;
			entries.Add((label, new Rect(client.Position.X, client.Position.Y + y, client.Size.Width, LineHeight)));
		}

		CheckSavegames();
		// The frontend sets the savegame path after the game is created.
		checkSavegames = true;
	}

	public override void Close()
	{
		Game.Destroy(background);
		background = null;

		base.Close();
	}

	public override void Update(long elapsedTicks)
	{
		base.Update(elapsedTicks);

		if (checkSavegames)
		{
			checkSavegames = false;
			CheckSavegames();
		}
	}

	void CheckSavegames()
	{
		enabled[ContinueIndex] = Game.HasSavegame;
		enabled[NewGameIndex] = true;
		enabled[LoadGameIndex] = Game.GetSavegameSlots().Count != 0;
		enabled[QuitIndex] = true;
		hoveredIndex = enabled[ContinueIndex] ? ContinueIndex : NewGameIndex;
		UpdateEntries();
	}

	void UpdateEntries()
	{
		var texts = Game.HasEnglishTexts ? EnglishTexts : GermanTexts;

		for (int i = 0; i < entries.Count; i++)
		{
			int color = !enabled[i] ? DisabledTextColorIndex : i == hoveredIndex ? HoveredTextColorIndex : TextColorIndex;
			entries[i].Label.SetText(texts[i], color, TextManager.TransparentPaper, Palette);
			entries[i].Label.Visible = true;
		}
	}

	void Hover(int index)
	{
		if (index != hoveredIndex && enabled[index])
		{
			hoveredIndex = index;
			UpdateEntries();
		}
	}

	void MoveHover(int direction)
	{
		int index = hoveredIndex;

		do
		{
			index = (index + direction + enabled.Length) % enabled.Length;
		}
		while (!enabled[index]);

		Hover(index);
	}

	/// <summary>
	/// Selects an entry. This is also used by scenario scripts.
	/// </summary>
	internal void Select(int index)
	{
		if (!enabled[index])
			return;

		switch (index)
		{
			case ContinueIndex:
				Game.TryLoadGameAndContinue(null);
				break;
			case NewGameIndex:
				Game.StartNewGame();
				break;
			case LoadGameIndex:
				Game.ScreenHandler.PushScreen(ScreenType.LoadGame);
				break;
			case QuitIndex:
				Game.Quit();
				break;
		}
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
				MoveHover(-1);
				return true;
			case Key.Down:
				MoveHover(1);
				return true;
			case Key.Enter:
				Select(hoveredIndex);
				return true;
		}

		return base.KeyDown(key, keyModifiers);
	}
}
