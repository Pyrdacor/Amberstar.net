using Amberstar.Frontend;

namespace Amberstar.Tests;

public class CharacterCreatorTests
{
	const int OkButtonIndex = 2;
	const int ExitButtonIndex = 5;
	const int RollButtonIndex = 6;
	const int FemaleButtonIndex = 7;

	[Fact]
	public void ExitReturnsToTheMainMenuAndDiscardsTheChanges()
	{
		using var game = new HeadlessGame(TestData.GetDataPath("German/AmberfilesST"), showMainMenu: true);
		game.Step(120);
		game.Game.ScriptSelectMainMenuEntry(1);
		game.Step(120);
		int? gold = game.Game.ScriptGetPartyMemberValue(1, "gold");

		Assert.Null(game.Game.ScriptPressButton(FemaleButtonIndex));
		game.Step(30);
		Assert.Null(game.Game.ScriptPressButton(RollButtonIndex));
		game.Step(30);
		Assert.Null(game.Game.ScriptPressButton(ExitButtonIndex));
		game.Step(120);

		Assert.Equal("MainMenu", game.Game.ScriptActiveScreen);
		Assert.EndsWith("Male 0", game.Game.ScriptHero);
		Assert.Equal(gold, game.Game.ScriptGetPartyMemberValue(1, "gold"));
	}

	[Theory]
	[InlineData("German/AmberfilesST")]
	[InlineData("English/AmberfilesAmiga")]
	public void NewGameStartsWithTheCharacterEditor(string folder)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder), showMainMenu: true);
		game.Step(120);
		game.Game.ScriptSelectMainMenuEntry(1);
		// the editor replaces the menu at once, a fade would show the layout in between
		Assert.Equal("CharacterCreation", game.Game.ScriptActiveScreen);
		Assert.False(game.Game.ScriptFading);
		game.Step(120);

		Assert.Null(game.Game.ScriptPressButton(OkButtonIndex));
		game.Step(120);
		// the first map starts with a text
		Assert.Equal("PictureText", game.Game.ScriptActiveScreen);
		Assert.EndsWith("Male 0", game.Game.ScriptHero);
	}
}
