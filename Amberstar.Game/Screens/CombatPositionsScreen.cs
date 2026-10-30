using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Not part of the original: the released source has only a dummy behind the button (Second_CIL in MAP.ASM).
/// The positions of the party in battles are chosen on the two party rows of the tactical
/// field. A click takes a member and the next click puts it there. A member who is
/// already there changes places with it.
/// </summary>
internal sealed class CombatPositionsScreen : ButtonGridScreen
{
	const int ExitButtonIndex = 2;
	const int Columns = 6;
	const int FirstPartyRow = 3;
	const int PartyRows = 2;
	const int CellSize = 16;
	const int CombatBackground = 1;
	const byte BackgroundDisplayLayer = 1;
	const byte TextDisplayLayer = 2;
	const byte IconDisplayLayer = 10;
	const byte MarkerDisplayLayer = 20;
	const int HeaderColorIndex = 8;
	const int TextColorIndex = 15;
	const int MarkerColorIndex = 1;
	static readonly Position fieldPosition = new(208, 49);
	static readonly Rect textArea = new(24, 70, 160, 80);
	readonly List<IColoredRect> marker = [];
	readonly Dictionary<int, Image> icons = [];
	IColoredRect? background;
	Label? text;
	int? selectedSlot = null;

	public override ScreenType Type { get; } = ScreenType.CombatPositions;

	public override bool AllowInventoryAccess => false;

	public override bool AllowCharacterSelection => false;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.GetCombatPaletteIndex(CombatBackground) ?? 0;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		var palette = ButtonGridPaletteIndex;
		Game.SetLayout(Layout.Battle, palette);
		Game.Cursor.CursorType = CursorType.Sword;

		background = Game.CreateColoredRect(Layer.UI, new(16, 49), new(176, 112), Game.PaletteColorProvider.GetPaletteColor(palette, 2));

		if (background != null)
			background.DisplayLayer = BackgroundDisplayLayer;

		var title = AddLabel(16, 55, 176, 7, TextDisplayLayer);
		title.SetText(TextManager.ToGameCharacters(Game.HasEnglishTexts ? "BATTLE POSITIONS" : "KAMPFPOSITIONEN"), HeaderColorIndex, TextManager.TransparentPaper, palette);
		title.Alignment = TextAlignment.Center;
		title.Visible = true;

		text = AddLabel(textArea.Position.X, textArea.Position.Y, textArea.Size.Width, textArea.Size.Height, TextDisplayLayer);
		text.Visible = true;

		icons.Clear();

		Game.ForeachPartyMemberSlot((index, partyMember) =>
		{
			if (partyMember != null)
			{
				var icon = AddImage(0, 0, CellSize, CellSize, Game.GraphicIndexProvider.GetTacticalIconIndex((int)partyMember.Class * 3), IconDisplayLayer);
				icon.PaletteIndex = palette;
				icons.Add(index, icon);
			}
		});

		selectedSlot = null;
		Update();
	}

	public override void Close()
	{
		ClearMarker();
		Game.Destroy(background);
		background = null;

		base.Close();
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
	}

	protected override void ButtonClicked(int index)
	{
		if (index == ExitButtonIndex)
			Game.ScreenHandler.PopScreen();
	}

	static Position GetCellPosition(int position) =>
		fieldPosition + new Position(position % Columns * CellSize, (FirstPartyRow + position / Columns) * CellSize);

	void ClearMarker()
	{
		marker.ForEach(rect => rect.Visible = false);
		marker.Clear();
	}

	void AddMarkerRect(int x, int y, int width, int height)
	{
		var rect = Game.CreateColoredRect(Layer.UI, new(x, y), new(width, height), Game.PaletteColorProvider.GetPaletteColor(ButtonGridPaletteIndex, MarkerColorIndex));

		if (rect != null)
		{
			rect.DisplayLayer = MarkerDisplayLayer;
			marker.Add(rect);
		}
	}

	void Update()
	{
		var positions = Game.State.CombatPositions;

		foreach (var (slot, icon) in icons)
		{
			icon.Position = GetCellPosition(positions[slot]);
			icon.Visible = true;
		}

		ClearMarker();

		if (selectedSlot is int selected)
		{
			var (x, y) = GetCellPosition(positions[selected]);
			AddMarkerRect(x, y, CellSize, 1);
			AddMarkerRect(x, y + CellSize - 1, CellSize, 1);
			AddMarkerRect(x, y + 1, 1, CellSize - 2);
			AddMarkerRect(x + CellSize - 1, y + 1, 1, CellSize - 2);
		}

		bool english = Game.HasEnglishTexts;
		string hint = selectedSlot is int slotIndex
			? $"{Game.State.GetPartyMember(slotIndex + 1)!.Name}: {(english ? "CLICK THE NEW POSITION." : "KLICKE AUF DIE NEUE POSITION.")}"
			: english ? "CLICK A PARTY MEMBER." : "KLICKE AUF EIN GRUPPENMITGLIED.";
		text!.SetText(Game.AssetProvider.TextLoader.FromString(TextManager.ToGameCharacters(hint)), textArea.Size.Width, TextColorIndex, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		text.Visible = true;
	}

	int? SlotAt(int position)
	{
		foreach (var slot in icons.Keys)
		{
			if (Game.State.CombatPositions[slot] == position)
				return slot;
		}

		return null;
	}

	/// <summary>
	/// A cell of the party rows was clicked (0 to 11). This is also used by scenario scripts.
	/// </summary>
	internal void CellClicked(int position)
	{
		var positions = Game.State.CombatPositions;

		if (selectedSlot is not int selected)
		{
			selectedSlot = SlotAt(position);
		}
		else
		{
			if (SlotAt(position) is int other)
				positions[other] = positions[selected];

			positions[selected] = position;
			selectedSlot = null;
		}

		Update();
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Right && selectedSlot != null)
		{
			selectedSlot = null;
			Update();
			return true;
		}

		if (buttons == MouseButtons.Left)
		{
			var field = new Rect(fieldPosition.X, fieldPosition.Y + FirstPartyRow * CellSize, Columns * CellSize, PartyRows * CellSize);

			if (field.Contains(position))
			{
				CellClicked((position.X - field.Position.X) / CellSize + (position.Y - field.Position.Y) / CellSize * Columns);
				return true;
			}
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}
}
