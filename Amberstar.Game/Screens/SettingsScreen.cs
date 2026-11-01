using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Not part of the original. A list of the settings of the remake. A click on an entry chooses its next value.
/// </summary>
internal sealed class SettingsScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 52;
	const int WindowWidthInTiles = 16;
	const int WindowHeightInTiles = 8;
	const byte WindowDisplayLayer = 130;
	const byte ControlDisplayLayer = 140;
	const int TitleHeight = 9;
	const byte TextColorIndex = 15;
	const int ResolutionEntry = 0;
	const int WindowModeEntry = 1;
	const int CampRegenerationEntry = 2;
	const int EntryCount = 3;
	const int ListTextPadding = 3;
	const int CharacterWidth = 6;
	static readonly int[] campRegenerations = [10, 50];
	List? list;

	public override ScreenType Type { get; } = ScreenType.Settings;

	public override bool AllowInventoryAccess => false;

	public override bool AllowCharacterSelection => false;

	public override void Init()
	{
		base.Init();

		var (clientWidth, clientHeight) = ClientArea.Size;
		var title = AddLabel(0, 0, clientWidth, TitleHeight, ControlDisplayLayer);
		title.SetText(Game.HasEnglishTexts ? "SETTINGS" : "EINSTELLUNGEN", TextColorIndex, TextManager.TransparentPaper, Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
		title.Alignment = TextAlignment.Center;
		title.Visible = true;

		list = AddList(0, TitleHeight + 2, clientWidth, clientHeight - TitleHeight - 2 - Button.Height, ControlDisplayLayer);
		list.ItemClicked += (index, _) => Select(index);

		for (int i = 0; i < EntryCount; i++)
			list.AddItem(GetText(i), TextColorIndex);

		var exitButton = AddButton(clientWidth - Button.Width, clientHeight - Button.Height, ButtonType.Exit, ControlDisplayLayer);
		exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();
	}

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		UpdateTexts();
	}

	string GetText(int entry)
	{
		var configuration = Game.Configuration;
		bool english = Game.HasEnglishTexts;

		var (name, value) = entry switch
		{
			ResolutionEntry => (english ? "RESOLUTION" : "AUFLÖSUNG", configuration.WindowMode == WindowMode.Window ? configuration.Resolutions[configuration.Resolution].ToString() : "-"),
			WindowModeEntry => (english ? "DISPLAY" : "ANZEIGE", configuration.WindowMode switch
			{
				WindowMode.Window => english ? "WINDOW" : "FENSTER",
				WindowMode.BorderlessWindow => english ? "FULLSCREEN WINDOW" : "VOLLBILD-FENSTER",
				_ => english ? "NATIVE" : "NATIV",
			}),
			_ => (english ? "CAMP REGENERATION" : "LAGER-REGENERATION", $"{configuration.CampRegeneration}%"),
		};
		// All characters have the same width, so spaces align the values to the right.
		int lineLength = (ClientArea.Size.Width - 2 * ListTextPadding) / CharacterWidth;

		return TextManager.ToGameCharacters($"{name}:".PadRight(lineLength - value.Length) + value);
	}

	/// <summary>
	/// Chooses the next value of an entry. This is also used by scenario scripts.
	/// </summary>
	internal void Select(int entry)
	{
		var configuration = Game.Configuration;

		switch (entry)
		{
			case ResolutionEntry:
				// The other modes use the size of the monitor.
				if (configuration.WindowMode != WindowMode.Window)
					return;

				configuration.Resolution = (configuration.Resolution + 1) % configuration.Resolutions.Length;
				break;
			case WindowModeEntry:
				Game.SelectNextWindowMode();
				return;
			case CampRegenerationEntry:
				configuration.CampRegeneration = campRegenerations[(Array.IndexOf(campRegenerations, configuration.CampRegeneration) + 1) % campRegenerations.Length];
				break;
			default:
				return;
		}

		UpdateTexts();
		Game.SettingsChanged();
	}

	internal void UpdateTexts()
	{
		for (int i = 0; i < EntryCount; i++)
			list!.SetItem(i, GetText(i), TextColorIndex);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left && list?.MouseClick(position) == true)
			return true;

		return base.MouseDown(position, buttons, keyModifiers);
	}
}
