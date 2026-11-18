using Amber.Assets.Common;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// The characters of a translation which the fonts of the game don't have (see <see cref="GlyphFile"/>).
///
/// The fonts map the characters (bytes) to their glyphs. Characters of the Atari ST character set (like é)
/// get their glyph at their place in the font. Other characters get a free place of the font (a byte which
/// the texts don't use), so they can also be stored in the names and texts of the game data (one byte per
/// character). Characters which show the glyph of another character are replaced by it.
/// </summary>
public sealed class CustomCharset
{
	// the special characters which all fonts have and the original texts use
	const string OriginalSpecialCharacters = "ÄÖÜäöüß";

	readonly Dictionary<char, char> gameCharacters = [];
	readonly Dictionary<char, char> textCharacters = [];
	readonly Dictionary<char, byte[]> glyphs = [];

	public CustomCharset(GlyphFile file)
	{
		File = file;
		var usedFontCharacters = new HashSet<char>(OriginalSpecialCharacters.Select(FontCharacters.ToFontCharacter));
		var ownGlyphs = file.Glyphs.Where(glyph => glyph.Rows != null).ToList();

		// Characters of the font character set and characters which can have their own byte keep it.
		foreach (var glyph in ownGlyphs)
		{
			var ch = glyph.Character;

			if (FontCharacters.UnicodeToFont.TryGetValue(ch, out var fontCharacter))
			{
				gameCharacters[ch] = ch;
				glyphs[fontCharacter] = glyph.GetRowBytes();
				usedFontCharacters.Add(fontCharacter);
			}
		}

		foreach (var glyph in ownGlyphs.Where(glyph => !gameCharacters.ContainsKey(glyph.Character)))
		{
			var ch = glyph.Character;

			if (IsFreeFontCharacter(ch, usedFontCharacters))
				Assign(glyph, ch, usedFontCharacters);
		}

		// The other characters get the free places of the font.
		var freeCharacters = Enumerable.Range(0x80, 0x80)
			.Select(code => (char)code)
			.OrderBy(code => FontCharacters.IsFontCharacterOfUnicode(code) ? 1 : 0) // unused places first
			.ThenBy(code => code)
			.Where(code => IsFreeFontCharacter(code, usedFontCharacters))
			.GetEnumerator();

		foreach (var glyph in ownGlyphs.Where(glyph => !gameCharacters.ContainsKey(glyph.Character)))
		{
			while (freeCharacters.MoveNext())
			{
				if (IsFreeFontCharacter(freeCharacters.Current, usedFontCharacters))
				{
					Assign(glyph, freeCharacters.Current, usedFontCharacters);
					break;
				}
			}

			if (!gameCharacters.ContainsKey(glyph.Character))
				UnassignedCharacters.Add(glyph.Character);
		}

		// references
		foreach (var glyph in file.Glyphs.Where(glyph => glyph.Reference != null))
		{
			var reference = glyph.Reference!.Value;
			gameCharacters[glyph.Character] = gameCharacters.TryGetValue(reference, out var target) ? target : reference;
		}
	}

	void Assign(GlyphDefinition glyph, char fontCharacter, HashSet<char> usedFontCharacters)
	{
		gameCharacters[glyph.Character] = fontCharacter;

		if (fontCharacter != glyph.Character)
			textCharacters[fontCharacter] = glyph.Character;

		glyphs[fontCharacter] = glyph.GetRowBytes();
		usedFontCharacters.Add(fontCharacter);
	}

	/// <summary>
	/// A place of the font is free if no text character is shown with it, it is not used yet and the
	/// encoding of the game data stores it as the same byte.
	/// </summary>
	static bool IsFreeFontCharacter(char ch, HashSet<char> usedFontCharacters)
	{
		if (ch < 0x80 || ch > 0xff || ch == 0xa0 || ch == 0xad || usedFontCharacters.Contains(ch) ||
			FontCharacters.UnicodeToFont.ContainsKey(ch))
			return false;

		var encoding = Amber.Serialization.DataReader.Encoding;
		var bytes = encoding.GetBytes(ch.ToString());

		return bytes.Length == 1 && bytes[0] == ch && encoding.GetString(bytes) == ch.ToString();
	}

	public GlyphFile File { get; }

	/// <summary>
	/// Characters which didn't get a place in the font (there are too many).
	/// </summary>
	public List<char> UnassignedCharacters { get; } = [];

	/// <summary>
	/// The characters of the font with a new glyph.
	/// </summary>
	public IReadOnlyCollection<char> FontCharactersWithGlyph => glyphs.Keys;

	/// <summary>
	/// The character of the game data for a character of the translation (or null if it is not part of the
	/// glyph file). Lowercase letters use the uppercase letter if they are not part of it.
	/// </summary>
	public char? ToGameCharacter(char ch)
	{
		if (gameCharacters.TryGetValue(ch, out var gameCharacter))
			return gameCharacter;

		var upper = char.ToUpperInvariant(ch);

		return upper != ch && gameCharacters.TryGetValue(upper, out gameCharacter) ? gameCharacter : null;
	}

	/// <summary>
	/// Replaces the characters of the glyph file by the characters of the game data.
	/// </summary>
	public string ToGameText(string text)
	{
		var chars = text.ToCharArray();

		for (int i = 0; i < chars.Length; i++)
		{
			if (ToGameCharacter(chars[i]) is char ch)
				chars[i] = ch;
		}

		return new string(chars);
	}

	/// <summary>
	/// The character of the translation for a character of the game data.
	/// </summary>
	public char ToTextCharacter(char gameCharacter) => textCharacters.TryGetValue(gameCharacter, out var ch) ? ch : gameCharacter;

	/// <summary>
	/// The new glyph of a character of the font in the format of the font (one byte per row) or null.
	/// </summary>
	public byte[]? GetGlyph(char fontCharacter) => glyphs.GetValueOrDefault(fontCharacter);

	public IGraphic? GetGlyphGraphic(char fontCharacter) =>
		glyphs.TryGetValue(fontCharacter, out var rows) ? Graphic.FromBitPlanes(GlyphDefinition.Width, GlyphDefinition.Height, rows, 1) : null;

	/// <summary>
	/// The glyph of a character of a text (for fonts which show the characters of the text directly like the
	/// font of the outro): the new glyph or the character whose glyph is shown.
	/// </summary>
	public (byte[]? Glyph, char Character) GetTextGlyph(char ch)
	{
		var gameCharacter = ToGameCharacter(ch) ?? ch;

		return (GetGlyph(FontCharacters.ToFontCharacter(gameCharacter)), ToTextCharacter(gameCharacter));
	}
}

/// <summary>
/// A font with the new glyphs of a translation.
/// </summary>
internal sealed class ExtendedFont(IFont font, CustomCharset charset) : IFont
{
	readonly Dictionary<char, IGraphic?> glyphs = [];

	public int Advance => font.Advance;
	public int GlyphWidth => font.GlyphWidth;
	public int GlyphHeight => font.GlyphHeight;
	public int LineHeight => font.LineHeight;

	public IGraphic? GetGlyph(char character, bool rune)
	{
		if (rune)
			return font.GetGlyph(character, rune);

		if (!glyphs.TryGetValue(character, out var glyph))
		{
			glyph = charset.GetGlyphGraphic(character) ?? font.GetGlyph(character, rune);
			glyphs.Add(character, glyph);
		}

		return glyph;
	}
}
