using Amberstar.Frontend;

namespace Amberstar.Tests;

public class CharacterCreatorTests
{
	const int OkButtonIndex = 2;

	[Theory]
	[InlineData("German/AmberfilesST")]
	[InlineData("English/AmberfilesAmiga")]
	public void NewGameStartsWithTheCharacterEditor(string folder)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder), showCharacterCreator: true);
		game.Step(120);
		Assert.Equal("CharacterCreation", game.Game.ScriptActiveScreen);

		Assert.Null(game.Game.ScriptPressButton(OkButtonIndex));
		game.Step(120);
		// the first map starts with a text
		Assert.Equal("PictureText", game.Game.ScriptActiveScreen);
		Assert.EndsWith("Male 0", game.Game.ScriptHero);
	}
}
