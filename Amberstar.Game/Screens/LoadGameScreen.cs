using System.Globalization;
using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Not part of the original. A scrollable list of the used savegame slots to choose one to load.
/// </summary>
internal sealed class LoadGameScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 52;
	const int WindowWidthInTiles = 16;
	const int WindowHeightInTiles = 8;
	const byte WindowDisplayLayer = 130;
	const byte ControlDisplayLayer = 140;
	const int TitleHeight = 9;
	const int TextColorIndex = 15;
	List<SavegameSlot> slots = [];
	List? list;

	public override ScreenType Type { get; } = ScreenType.LoadGame;

	public override bool AllowInventoryAccess => false;

	public override bool AllowCharacterSelection => false;

	internal int ScriptSlotCount => slots.Count;

	public override void Init()
	{
		base.Init();

		var (clientWidth, clientHeight) = ClientArea.Size;
		var title = AddLabel(0, 0, clientWidth, TitleHeight, ControlDisplayLayer);
		title.SetText(Game.HasEnglishTexts ? "LOAD GAME" : "SPIEL LADEN", TextColorIndex, TextManager.TransparentPaper, Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
		title.Alignment = TextAlignment.Center;
		title.Visible = true;

		list = AddList(0, TitleHeight, clientWidth, clientHeight - TitleHeight - Button.Height, ControlDisplayLayer, backgroundColorIndex: 3);
		list.ItemClicked += (index, _) => Select(index);

		int x = 0;
		int y = clientHeight - Button.Height;

		var upButton = AddButton(ref x, y, ButtonType.ArrowUp, ControlDisplayLayer);
		upButton.ClickAction += () => list?.Scroll(-1);
		upButton.RightClickAction += () => list?.ScrollToBegin();

		var downButton = AddButton(ref x, y, ButtonType.ArrowDown, ControlDisplayLayer);
		downButton.ClickAction += () => list?.Scroll(1);
		downButton.RightClickAction += () => list?.ScrollToEnd();

		var exitButton = AddButton(clientWidth - Button.Width, y, ButtonType.Exit, ControlDisplayLayer);
		exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();
	}

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		// the newest savegames first
		slots = [.. Game.GetSavegameSlots().OrderByDescending(slot => slot.SaveTime).ThenBy(slot => slot.Slot)];
		list!.ScrollToBegin();
		list.Clear();

		foreach (var slot in slots)
			list.AddItem(TextManager.ToGameCharacters($"{slot.Name.PadRight(Game.MaxSavegameNameLength)} {slot.SaveTime.ToString("dd.MM.yy HH:mm", CultureInfo.InvariantCulture)}"));
	}

	/// <summary>
	/// Loads the savegame of a list entry. This is also used by scenario scripts.
	/// </summary>
	internal void Select(int index) => Game.TryLoadGameAndContinue(slots[index].Path);

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		switch (key)
		{
			case Key.Up:
				list?.Scroll(-1);
				return true;
			case Key.Down:
				list?.Scroll(1);
				return true;
			case Key.PageUp:
				list?.Scroll(-10);
				return true;
			case Key.PageDown:
				list?.Scroll(10);
				return true;
			case Key.Home:
				list?.ScrollToBegin();
				return true;
			case Key.End:
				list?.ScrollToEnd();
				return true;
		}

		return base.KeyDown(key, keyModifiers);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left && list?.MouseClick(position) == true)
			return true;

		return base.MouseDown(position, buttons, keyModifiers);
	}

	public override bool MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
	{
		list?.MouseWheel(-Math.Sign(scrollY));

		return true;
	}
}
