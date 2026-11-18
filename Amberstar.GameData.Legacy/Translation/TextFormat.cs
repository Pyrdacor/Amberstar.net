using System.Text;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// Converts the texts of the game data (lists of words of the dictionary) to normal text for the
/// translators and back.
///
/// The words are separated by spaces. There is no space in front of punctuation and after an
/// opening bracket like the game shows it. A line break of the text is a line break and a new
/// paragraph is {P}. The characters are converted with <see cref="GameCharset"/>.
/// </summary>
internal static class TextFormat
{
	public const string ParagraphToken = "{P}";

	public static string Format(IEnumerable<word> words, List<string> dictionary)
	{
		var markers = Text.GetMarkers(dictionary);
		var builder = new StringBuilder();
		bool spaceNeeded = false;

		foreach (var index in words)
		{
			if (index == 0 || index >= dictionary.Count)
				continue;

			if (index == markers.CarriageReturn)
			{
				builder.Append('\n');
				spaceNeeded = false;
			}
			else if (index == markers.ParagraphMarker)
			{
				builder.Append(ParagraphToken);
				spaceNeeded = false;
			}
			else
			{
				if (spaceNeeded && !markers.EndPunctuation.Contains(index))
					builder.Append(' ');

				builder.Append(GameCharset.ToText(dictionary[index]));
				spaceNeeded = index != markers.OpenBracket;
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Splits the text into words of the dictionary. <paramref name="findWord"/> gives the index of a word
	/// and <paramref name="findOrAddWord"/> adds it if it is not part of the dictionary. Characters which are not part of the character set of the game are
	/// added to <paramref name="invalidCharacters"/>.
	/// </summary>
	public static List<word> Parse(string text, List<string> dictionary, Func<string, word?> findWord, Func<string, word> findOrAddWord, ISet<char>? invalidCharacters = null, CustomCharset? charset = null)
	{
		var markers = Text.GetMarkers(dictionary);
		var words = new List<word>();
		var endPunctuation = markers.EndPunctuation.ToDictionary(index => dictionary[index][0], index => (word)index);
		text = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace(ParagraphToken, "\u0001");

		void AddChunk(string chunk)
		{
			if (chunk.Length == 0)
				return;

			string gameString = GameCharset.ToGameString(chunk, invalidCharacters, charset);

			if (gameString.Length == 0)
				return;

			// A whole word of the dictionary (e.g. "MR." or "(1)") is kept.
			if (findWord(gameString) is word index)
			{
				words.Add(index);
				return;
			}

			var trailing = new Stack<word>();

			if (markers.OpenBracket > 0 && gameString.Length > 1 && gameString[0] == '(')
			{
				words.Add((word)markers.OpenBracket);
				gameString = gameString[1..];
			}

			while (gameString.Length > 1 && endPunctuation.TryGetValue(gameString[^1], out var punctuation))
			{
				trailing.Push(punctuation);
				gameString = gameString[..^1];
			}

			words.Add(findOrAddWord(gameString));

			while (trailing.Count != 0)
				words.Add(trailing.Pop());
		}

		var lines = text.Split('\n');

		for (int l = 0; l < lines.Length; l++)
		{
			if (l != 0)
				words.Add((word)markers.CarriageReturn);

			var paragraphs = lines[l].Split('\u0001');

			for (int p = 0; p < paragraphs.Length; p++)
			{
				if (p != 0)
					words.Add((word)markers.ParagraphMarker);

				foreach (var chunk in paragraphs[p].Split(' ', '\t'))
					AddChunk(chunk);
			}
		}

		return words;
	}
}
