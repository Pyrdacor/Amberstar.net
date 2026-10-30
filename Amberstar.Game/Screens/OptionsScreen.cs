using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The window to enter the name of a savegame.
/// </summary>
internal sealed class SavegameNameScreen : InputWordScreen
{
	public override ScreenType Type { get; } = ScreenType.InputSavegameName;

	protected override IText LabelText => Game.AssetProvider.TextLoader.FromString(Game.HasEnglishTexts ? "ENTER NAME :" : "GIB NAME EIN :");

	protected override int? MaxInputLength => Game.MaxSavegameNameLength;

	protected override string InitialText => (Game.ScreenHandler.ActiveScreen as OptionsScreen ?? Game.ScreenHandler.LastScreen as OptionsScreen)?.CurrentSlotName ?? "";

	protected override Func<char, bool>? CharacterFilter => Game.IsValidSavegameNameCharacter;
}

/// <summary>
/// The options menu (Options_menu in EYE.ASM of the original): quit the game,
/// switch the music on or off and save or load the game. The original has only one
/// savegame, here a slot is chosen in a list first. Saving and loading need a chosen slot.
/// </summary>
internal sealed class OptionsScreen : ButtonGridScreen
{
	const int QuitButtonIndex = 0;
	const int ExitButtonIndex = 2;
	const int MusicOnButtonIndex = 3;
	const int MusicOffButtonIndex = 4;
	const int SaveButtonIndex = 6;
	const int LoadButtonIndex = 7;
	const int DismissButtonIndex = 8;
	const byte ControlDisplayLayer = 20;
	const byte TextColorIndex = 15;
	const byte SelectedTextColorIndex = 1;
	static readonly Rect listArea = new(16, 58, 176, 112);
	List? list;
	Label? title;
	Button? upButton;
	Button? downButton;
	Dictionary<int, string> names = [];
	readonly bool[] used = new bool[Game.SavegameSlotCount];
	int? selectedSlot = null;

	public override ScreenType Type { get; } = ScreenType.Options;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Game.SetLayout(Layout.Save, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;

		title = AddLabel(16, 50, 176, 7, ControlDisplayLayer);
		title.SetText(TextManager.ToGameCharacters(Game.HasEnglishTexts ? "SAVEGAMES" : "SPIELST\u00c4NDE"), TextColorIndex, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		title.Alignment = TextAlignment.Center;
		title.Visible = true;

		list = AddList(listArea.Position.X, listArea.Position.Y, listArea.Size.Width, listArea.Size.Height, ControlDisplayLayer);
		list.ItemClicked += (index, _) => SelectSlot(index + 1);

		upButton = AddButton(128, 176, ButtonType.ArrowUp, ControlDisplayLayer);
		upButton.ClickAction += () => list?.Scroll(-1);
		upButton.RightClickAction += () => list?.ScrollToBegin();

		downButton = AddButton(160, 176, ButtonType.ArrowDown, ControlDisplayLayer);
		downButton.ClickAction += () => list?.Scroll(1);
		downButton.RightClickAction += () => list?.ScrollToEnd();

		selectedSlot = null;
		LoadSlots();

		for (int slot = 1; slot <= Game.SavegameSlotCount; slot++)
			list.AddItem(GetSlotText(slot), TextColorIndex);

		RequestButtonSetup();
	}

	/// <summary>
	/// The window to dismiss party members uses the same area as the list of the savegames.
	/// </summary>
	void ShowSavegames(bool show)
	{
		title!.Visible = show;
		list!.Visible = show;
		upButton!.Visible = show;
		downButton!.Visible = show;
	}

	public override void ScreenPushed(Screen screen)
	{
		base.ScreenPushed(screen);

		if (screen is DismissPartyMembersScreen)
			ShowSavegames(false);
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (screen is DismissPartyMembersScreen)
			ShowSavegames(true);

		if (screen is SavegameNameScreen && Game.CurrentWord != null && selectedSlot is int slot &&
			Game.SaveGameToSlot(slot, Game.CurrentWord))
		{
			LoadSlots();
			UpdateSlot(slot);
		}

		RequestButtonSetup();
	}

	void LoadSlots()
	{
		names = Game.LoadSavegameNames();
		Array.Clear(used);

		foreach (var slot in Game.GetSavegameSlots())
			used[slot.Slot - 1] = true;
	}

	string GetSlotText(int slot)
	{
		var name = used[slot - 1] ? Game.GetSavegameName(names, slot) : Game.HasEnglishTexts ? "(empty)" : "(leer)";

		return TextManager.ToGameCharacters($"{slot:00} {name}");
	}

	void UpdateSlot(int slot) =>
		list?.SetItem(slot - 1, GetSlotText(slot), slot == selectedSlot ? SelectedTextColorIndex : TextColorIndex);

	/// <summary>
	/// Chooses a slot (1-based). This is also used by scenario scripts.
	/// </summary>
	internal void SelectSlot(int slot)
	{
		int? previousSlot = selectedSlot;
		selectedSlot = slot;

		if (previousSlot != null)
			UpdateSlot(previousSlot.Value);

		UpdateSlot(slot);
		RequestButtonSetup();
	}

	internal string ScriptSlotText(int slot) => GetSlotText(slot);

	internal int ScriptVisibleSlots => list?.ScriptVisibleItems ?? 0;

	/// <summary>
	/// The name of a used slot is the suggestion for a new name.
	/// </summary>
	internal string CurrentSlotName => selectedSlot is int slot && used[slot - 1] && names.TryGetValue(slot, out var name) ? name : "";

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

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		buttonGrid.SetButton(QuitButtonIndex, ButtonType.Quit);
		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(MusicOnButtonIndex, ButtonType.Music);
		buttonGrid.SetButton(MusicOffButtonIndex, ButtonType.NoMusic);
		buttonGrid.SetButton(SaveButtonIndex, ButtonType.Save);
		buttonGrid.SetButton(LoadButtonIndex, ButtonType.Load);
		buttonGrid.SetButton(DismissButtonIndex, ButtonType.DismissPartyMembers);

		buttonGrid.EnableButton(MusicOnButtonIndex, Game.State.MusicBlock);
		buttonGrid.EnableButton(MusicOffButtonIndex, !Game.State.MusicBlock);
		buttonGrid.EnableButton(SaveButtonIndex, Game.SavegamePath != null && selectedSlot != null);
		buttonGrid.EnableButton(LoadButtonIndex, selectedSlot is int slot && used[slot - 1]);
		buttonGrid.EnableButton(DismissButtonIndex, Game.State.PartyMembersWithSlot.Any(member => member.SlotIndex != 0 && member.PartyMember != null));
	}

	protected override void ButtonClicked(int index)
	{
		switch (index)
		{
			case QuitButtonIndex:
				Game.AskForConfirmation(Game.LoadMessageText(Message.ReallyQuitProgram), () => Game.Quit());
				break;
			case ExitButtonIndex:
				Game.ScreenHandler.PopScreen();
				break;
			case MusicOnButtonIndex:
			case MusicOffButtonIndex:
				Game.SetMusic(index == MusicOnButtonIndex);
				RequestButtonSetup();
				break;
			case SaveButtonIndex:
				Game.ScreenHandler.PushScreen(ScreenType.InputSavegameName);
				break;
			case DismissButtonIndex:
				Game.ScreenHandler.PushScreen(ScreenType.DismissPartyMembers);
				break;
			case LoadButtonIndex:
				Game.AskForConfirmation(Game.LoadMessageText(Message.LoadGame), () => Game.TryLoadGameAndContinue(Game.GetSavegameSlotPath(selectedSlot!.Value)));
				break;
		}
	}
}
