using Amberstar.Frontend;

namespace Amberstar.Tests;

public class MainMenuTests
{
	const int ContinueEntry = 0;
	const int LoadGameEntry = 2;
	const int QuitEntry = 3;

	static HeadlessGame CreateGame()
	{
		var game = new HeadlessGame(TestData.GetDataPath("German/AmberfilesST"), showMainMenu: true);
		game.Step(120);
		return game;
	}

	[Theory]
	[InlineData("English/AmberfilesAmiga", true)]
	[InlineData("English/AmberfilesST", true)]
	[InlineData("German/AmberfilesAmiga", false)]
	[InlineData("German/AmberfilesST", false)]
	public void TheLanguageOfTheDataIsDetected(string folder, bool english)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder));
		Assert.Equal(english, game.Game.ScriptHasEnglishTexts);
	}

	[Fact]
	public void WithoutSavegamesContinueAndLoadAreDisabled()
	{
		using var game = CreateGame();
		Assert.Equal("MainMenu", game.Game.ScriptActiveScreen);
		Assert.Equal("0101", game.Game.ScriptMainMenuEntries);

		game.Game.ScriptSelectMainMenuEntry(ContinueEntry);
		game.Game.ScriptSelectMainMenuEntry(LoadGameEntry);
		game.Step(120);
		Assert.Equal("MainMenu", game.Game.ScriptActiveScreen);
	}

	[Fact]
	public void ContinueLoadsTheContinueSavegame()
	{
		using var game = CreateGame();
		Assert.Null(game.Game.ScriptSetPartyMemberValue(1, "gold", 1234));
		game.Game.SaveGame();
		game.Game.ScriptSetPartyMemberValue(1, "gold", 1);
		game.Game.ScriptShowMainMenuAgain();
		game.Step(120);
		Assert.Equal("1101", game.Game.ScriptMainMenuEntries);

		game.Game.ScriptSelectMainMenuEntry(ContinueEntry);
		game.Step(300);
		Assert.Equal(1234, game.Game.ScriptGetPartyMemberValue(1, "gold"));
		Assert.NotEqual("MainMenu", game.Game.ScriptActiveScreen);
	}

	[Fact]
	public void LoadShowsTheUsedSlotsAndLoadsTheChosenOne()
	{
		using var game = CreateGame();
		game.Game.ScriptSetPartyMemberValue(1, "gold", 111);
		game.Game.SaveGame(game.Game.ScriptSavegameSlotPath(1));
		game.Game.ScriptSetPartyMemberValue(1, "gold", 222);
		game.Game.SaveGame(game.Game.ScriptSavegameSlotPath(30));
		Assert.EndsWith("Savegame_30.sav", game.Game.ScriptSavegameSlotPath(30));
		// the newest savegame is the first entry, whatever its slot is
		File.SetLastWriteTime(game.Game.ScriptSavegameSlotPath(1), new DateTime(2026, 1, 1));
		File.SetLastWriteTime(game.Game.ScriptSavegameSlotPath(30), new DateTime(2026, 1, 2));
		game.Game.ScriptSetPartyMemberValue(1, "gold", 1);
		game.Game.ScriptShowMainMenuAgain();
		game.Step(120);
		Assert.Equal("0111", game.Game.ScriptMainMenuEntries);

		game.Game.ScriptSelectMainMenuEntry(LoadGameEntry);
		game.Step(120);
		Assert.Equal("LoadGame", game.Game.ScriptActiveScreen);
		Assert.Equal(2, game.Game.ScriptLoadGameSlotCount);

		game.Game.ScriptSelectLoadGameEntry(0);
		game.Step(300);
		Assert.Equal(222, game.Game.ScriptGetPartyMemberValue(1, "gold"));
	}

	[Fact]
	public void ADamagedSavegameShowsAMessage()
	{
		using var game = CreateGame();
		game.Game.SaveGame();
		File.WriteAllBytes(game.Game.SavegamePath!, [1, 2, 3, 4, 5, 6, 7, 8]);

		game.Game.ScriptShowMainMenuAgain();
		game.Step(120);
		game.Game.ScriptSelectMainMenuEntry(ContinueEntry);
		game.Step(120);
		Assert.Equal("TextBox", game.Game.ScriptActiveScreen);
	}

	[Fact]
	public void SavegameNamesHaveADefaultAndAreLimited()
	{
		using var game = CreateGame();
		game.Game.SaveGame(game.Game.ScriptSavegameSlotPath(1));
		game.Game.SaveGame(game.Game.ScriptSavegameSlotPath(2));
		game.Game.SaveGame(game.Game.ScriptSavegameSlotPath(3));

		// without the file
		Assert.Equal(["1:Save 01", "2:Save 02", "3:Save 03"], game.Game.ScriptLoadGameEntries);

		File.WriteAllLines(game.Game.ScriptSavegameNamesPath,
		[
			"01=  \u00c4rger im Wald  ",
			"02=<bad>/name:",
			"03=123456789012345678901234567890",
			"nonsense",
			"31=too high",
		]);

		Assert.Equal(["1:\u00c4rger im Wald", "2:badname", "3:12345678901234567890"], game.Game.ScriptLoadGameEntries);
	}

	[Fact]
	public void SavingToASlotAlsoWritesTheContinueSavegameAndTheName()
	{
		using var game = new HeadlessGame(TestData.GetDataPath("German/AmberfilesST"));
		game.Step(120);
		Assert.False(game.Game.HasSavegame);

		Assert.True(game.Game.ScriptSaveToSlot(5, "  Mein Spiel  "));
		Assert.False(game.Game.ScriptSaveToSlot(6, "   "));

		Assert.True(game.Game.HasSavegame);
		Assert.Equal(File.ReadAllBytes(game.Game.ScriptSavegameSlotPath(5)), File.ReadAllBytes(game.Game.SavegamePath!));
		Assert.False(File.Exists(game.Game.ScriptSavegameSlotPath(6)));
		Assert.Equal(["05=Mein Spiel"], File.ReadAllLines(game.Game.ScriptSavegameNamesPath));
	}

	[Fact]
	public void QuitEndsTheGame()
	{
		var game = CreateGame();
		game.Game.ScriptSelectMainMenuEntry(QuitEntry);
		Assert.True(game.ExitRequested);
	}
}
