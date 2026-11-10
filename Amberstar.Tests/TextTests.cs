using Amber.Common;
using Amberstar.GameData.Serialization;
using Amberstar.GameData;

namespace Amberstar.Tests;

public class TextTests
{
	[Theory]
	[InlineData("German/AmberfilesST")]
	[InlineData("English/AmberfilesST")]
	[InlineData("German/AmberfilesAmiga")]
	[InlineData("English/AmberfilesAmiga")]
	[InlineData("German/AmberfilesDOS")]
	[InlineData("English/AmberfilesDOS")]
	public void PunctuationFollowsTheWordAndControlWordsAreNotPrinted(string folder)
	{
		var textLoader = TestData.CreateAssetProvider(folder).TextLoader;
		var errors = new List<string>();

		foreach (var message in Enum.GetValues<Message>())
		{
			var lines = textLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)message)).GetLines(40);

			foreach (var line in lines)
			{
				// the original removes the space in front of these words (EndPuncts in WORDS.ASM)
				if (new[] { " .", " ,", " :", " ?", " !" }.Any(line.Contains) || line.Contains('#') || line.Contains('$'))
					errors.Add($"{message}: '{line}'");
			}
		}

		Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(20)));
	}
}
