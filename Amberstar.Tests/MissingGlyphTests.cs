using Amberstar.Frontend;

namespace Amberstar.Tests;

public sealed class MissingGlyphTests
{
	[Theory]
	[InlineData("German/AmberfilesAmiga", "~LA\u00df~")] // the rune font of the Amiga version has no sharp s (text of Firlas)
	[InlineData("English/AmberfilesAmiga", "COSTS 5$")] // the text font has no dollar (text of map 120)
	public void TextsWithMissingGlyphsCanBeShown(string folder, string text)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder));

		game.Game.TextManager.Create(text).Show(0, 0, 100);
	}

	[Theory]
	[InlineData("German/AmberfilesAmiga")]
	[InlineData("English/AmberfilesAmiga")]
	public void TheAmigaVersionsUseTheSharpSRuneOfTheAtariSTVersions(string folder)
	{
		using var amiga = new HeadlessGame(TestData.GetDataPath(folder));
		using var atariST = new HeadlessGame(TestData.GetDataPath("German/AmberfilesST"));

		var rune = amiga.Game.AssetProvider.FontLoader.LoadFont().GetGlyph('\u009e', true);
		var expectedRune = atariST.Game.AssetProvider.FontLoader.LoadFont().GetGlyph('\u009e', true);

		Assert.NotNull(rune);
		Assert.Equal(expectedRune!.GetData().ToArray(), rune.GetData().ToArray());
	}
}
