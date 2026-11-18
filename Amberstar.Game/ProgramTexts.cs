namespace Amberstar.Game;

/// <summary>
/// Texts of the program which are not part of the game data (the menus and messages of the remake).
/// </summary>
public enum ProgramText
{
	MainMenuContinueGame,
	MainMenuStartNewGame,
	MainMenuLoadGame,
	MainMenuQuit,
	LoadGameTitle,
	SettingsTitle,
	SettingsResolution,
	SettingsDisplay,
	SettingsWindow,
	SettingsFullscreenWindow,
	SettingsNative,
	SettingsCampRegeneration,
	SavegamesTitle,
	EmptySavegameSlot,
	EnterName,
	GameSaved,
	SavegameNotLoaded,
	CheatModeOn,
	BattlePositionsTitle,
	ClickNewPosition,
	ClickPartyMember,
	CrumbledToDust,
	BurnedToAshes,
	SpellHasNoEffect,
}

public static class ProgramTexts
{
	/// <summary>
	/// The English and German texts. {0} is replaced by a value (e.g. the name of a party member).
	/// </summary>
	public static IReadOnlyDictionary<ProgramText, (string English, string German, string Note)> Texts { get; } =
		new Dictionary<ProgramText, (string English, string German, string Note)>
		{
			{ ProgramText.MainMenuContinueGame, ("CONTINUE GAME", "SPIEL FORTSETZEN", "Entry of the main menu.") },
			{ ProgramText.MainMenuStartNewGame, ("START NEW GAME", "NEUES SPIEL STARTEN", "Entry of the main menu.") },
			{ ProgramText.MainMenuLoadGame, ("LOAD GAME", "SPIEL LADEN", "Entry of the main menu.") },
			{ ProgramText.MainMenuQuit, ("QUIT", "BEENDEN", "Entry of the main menu.") },
			{ ProgramText.LoadGameTitle, ("LOAD GAME", "SPIEL LADEN", "Title of the window with the savegames.") },
			{ ProgramText.SettingsTitle, ("SETTINGS", "EINSTELLUNGEN", "Title of the settings window.") },
			{ ProgramText.SettingsResolution, ("RESOLUTION", "AUFLÖSUNG", "Setting (the name and the value must fit in one line).") },
			{ ProgramText.SettingsDisplay, ("DISPLAY", "ANZEIGE", "Setting (the name and the value must fit in one line).") },
			{ ProgramText.SettingsWindow, ("WINDOW", "FENSTER", "Value of the setting DISPLAY.") },
			{ ProgramText.SettingsFullscreenWindow, ("FULLSCREEN WINDOW", "VOLLBILD-FENSTER", "Value of the setting DISPLAY.") },
			{ ProgramText.SettingsNative, ("NATIVE", "NATIV", "Value of the setting DISPLAY.") },
			{ ProgramText.SettingsCampRegeneration, ("CAMP REGENERATION", "LAGER-REGENERATION", "Setting (the name and the value must fit in one line).") },
			{ ProgramText.SavegamesTitle, ("SAVEGAMES", "SPIELSTÄNDE", "Title of the savegame window of the options.") },
			{ ProgramText.EmptySavegameSlot, ("(empty)", "(leer)", "An empty savegame slot.") },
			{ ProgramText.EnterName, ("ENTER NAME :", "GIB NAME EIN :", "Label of the name input (savegames and new character).") },
			{ ProgramText.GameSaved, ("The game was saved.", "Der Spielstand wurde gespeichert.", "Message of the quick save.") },
			{ ProgramText.SavegameNotLoaded, ("The savegame could not be loaded.", "Der Spielstand konnte nicht geladen werden.", "Error message.") },
			{ ProgramText.CheatModeOn, ("CHEAT MODE ON. CTRL+D SELECTS THE MUSIC.", "CHEATMODUS AN. STRG+D WÄHLT DIE MUSIK.", "Message of the cheat mode.") },
			{ ProgramText.BattlePositionsTitle, ("BATTLE POSITIONS", "KAMPFPOSITIONEN", "Title of the battle positions window.") },
			{ ProgramText.ClickNewPosition, ("CLICK THE NEW POSITION.", "KLICKE AUF DIE NEUE POSITION.", "Hint of the battle positions window.") },
			{ ProgramText.ClickPartyMember, ("CLICK A PARTY MEMBER.", "KLICKE AUF EIN GRUPPENMITGLIED.", "Hint of the battle positions window.") },
			{ ProgramText.CrumbledToDust, ("{0} HAS CRUMBLED TO DUST.", "{0} IST ZU STAUB ZERFALLEN.", "Spell hint, {0} is the name of the party member.") },
			{ ProgramText.BurnedToAshes, ("{0} HAS BEEN BURNED TO ASHES.", "{0} IST ZU ASCHE VERBRANNT.", "Spell hint, {0} is the name of the party member.") },
			{ ProgramText.SpellHasNoEffect, ("THE SPELL HAS NO EFFECT ON {0}.", "DER SPRUCH WIRKT BEI {0} NICHT.", "Spell hint, {0} is the name of the party member.") },
		};
}
