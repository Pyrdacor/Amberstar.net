using System.Globalization;
using System.Text;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// Converts the strings of the game data to normal text and back. The strings are read with the encoding
/// of the data reader which converts the special characters (like the German umlauts) of all versions to
/// Unicode, so the texts of all versions are the same. Control codes (e.g. the color codes of the UI texts)
/// and other characters which can't be shown are written as {XX} with the hexadecimal value of the byte.
/// </summary>
public static class GameCharset
{
	// The renderer of the game maps these characters to the glyphs of the Atari ST character set (0x80 to 0xbf).
	const string AtariCharacters =
		"ÇüéâäàåçêëèïîìÄÅ" +
		"ÉæÆôöòûùÿÖÜ¢£¥ßƒ" +
		"áíóúñÑªº¿⌐¬½¼¡«»" +
		"ãõØøœŒÀÃÕ¨´†¶©®™";

	static bool IsPlainCharacter(char ch) => (ch >= 0x20 && ch < 0x7f && ch != '{' && ch != '}') || ch > 0xa0 || AtariCharacters.Contains(ch);

	/// <summary>
	/// Converts a string of the game data to normal text.
	/// </summary>
	public static string ToText(string gameString)
	{
		var builder = new StringBuilder(gameString.Length);

		foreach (char ch in gameString)
		{
			if (IsPlainCharacter(ch) && ch != 0xad)
				builder.Append(ch);
			else
				builder.Append($"{{{(int)ch:X2}}}");
		}

		return builder.ToString();
	}

	/// <summary>
	/// Converts normal text to a string of the game data. Characters which can't be stored in the game data (one
	/// byte per character) are added to <paramref name="invalidCharacters"/> and skipped.
	/// </summary>
	public static string ToGameString(string text, ISet<char>? invalidCharacters = null)
	{
		var builder = new StringBuilder(text.Length);

		for (int i = 0; i < text.Length; i++)
		{
			char ch = text[i];

			if (ch == '{' && i + 3 < text.Length && text[i + 3] == '}' &&
				byte.TryParse(text.AsSpan(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
			{
				builder.Append((char)value);
				i += 3;
			}
			else if (ch >= 0x20 && (ch <= 0xff || AtariCharacters.Contains(ch)))
				builder.Append(ch);
			else
				invalidCharacters?.Add(ch);
		}

		return builder.ToString();
	}

	/// <summary>
	/// The bytes of a string of the game data (the encoding of the data reader).
	/// </summary>
	public static byte[] ToBytes(string gameString) => Amber.Serialization.DataReader.Encoding.GetBytes(gameString);

	/// <summary>
	/// The character of the font of the game for a character (the font has the Atari ST character set).
	/// </summary>
	public static char ToFontCharacter(char ch)
	{
		int index = AtariCharacters.IndexOf(ch);

		return index == -1 ? ch : (char)(0x80 + index);
	}

	/// <summary>
	/// The characters of the text which the game font can't show (control codes and spaces are always valid).
	/// </summary>
	public static IEnumerable<string> GetCharactersWithoutGlyph(string gameString, IFont font) => gameString
		.Where(ch => ch > ' ' && font.GetGlyph(ToFontCharacter(ch), false) == null)
		.Distinct()
		.Select(ch => ToText(ch.ToString()));
}
