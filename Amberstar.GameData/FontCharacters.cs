namespace Amberstar.GameData;

/// <summary>
/// The fonts of the game have the glyphs of the Atari ST character set. The texts are read as Unicode
/// (the encoding of the data reader converts the special characters), so the renderer maps these
/// characters to the characters of the font.
/// </summary>
public static class FontCharacters
{
	public static IReadOnlyDictionary<char, char> UnicodeToFont { get; } = new Dictionary<char, char>
	{
		{ 'Ç', (char)0x80 },
		{ 'ü', (char)0x81 },
		{ 'é', (char)0x82 },
		{ 'â', (char)0x83 },
		{ 'ä', (char)0x84 },
		{ 'à', (char)0x85 },
		{ 'å', (char)0x86 },
		{ 'ç', (char)0x87 },
		{ 'ê', (char)0x88 },
		{ 'ë', (char)0x89 },
		{ 'è', (char)0x8A },
		{ 'ï', (char)0x8B },
		{ 'î', (char)0x8C },
		{ 'ì', (char)0x8D },
		{ 'Ä', (char)0x8E },
		{ 'Å', (char)0x8F },
		{ 'É', (char)0x90 },
		{ 'æ', (char)0x91 },
		{ 'Æ', (char)0x92 },
		{ 'ô', (char)0x93 },
		{ 'ö', (char)0x94 },
		{ 'ò', (char)0x95 },
		{ 'û', (char)0x96 },
		{ 'ù', (char)0x97 },
		{ 'ÿ', (char)0x98 },
		{ 'Ö', (char)0x99 },
		{ 'Ü', (char)0x9A },
		{ '¢', (char)0x9B },
		{ '£', (char)0x9C },
		{ '¥', (char)0x9D },
		{ 'ß', (char)0x9E },
		{ 'ƒ', (char)0x9F },
		{ 'á', (char)0xA0 },
		{ 'í', (char)0xA1 },
		{ 'ó', (char)0xA2 },
		{ 'ú', (char)0xA3 },
		{ 'ñ', (char)0xA4 },
		{ 'Ñ', (char)0xA5 },
		{ '¿', (char)0xA8 },
		{ '¬', (char)0xAA },
		{ '¡', (char)0xAD },
		{ '«', (char)0xAE },
		{ '»', (char)0xAF },
		{ 'ã', (char)0xB0 },
		{ 'õ', (char)0xB1 },
		{ 'Ø', (char)0xB2 },
		{ 'ø', (char)0xB3 },
		{ 'œ', (char)0xB4 },
		{ 'Œ', (char)0xB5 },
		{ 'À', (char)0xB6 },
		{ 'Ã', (char)0xB7 },
		{ 'Õ', (char)0xB8 },
		{ 'ĳ', (char)0xC0 },
		{ 'Ĳ', (char)0xC1 },
		{ '§', (char)0xDD },
		{ '°', (char)0xF8 },
		{ '²', (char)0xFD },
		{ '³', (char)0xFE },
	};

	static readonly HashSet<char> fontCharactersOfUnicode = [.. UnicodeToFont.Values];

	/// <summary>
	/// The character of the font which shows the given character.
	/// </summary>
	public static char ToFontCharacter(char ch) => UnicodeToFont.TryGetValue(ch, out var fontCharacter) ? fontCharacter : ch;

	/// <summary>
	/// True if another character is shown with this character of the font.
	/// </summary>
	public static bool IsFontCharacterOfUnicode(char fontCharacter) => fontCharactersOfUnicode.Contains(fontCharacter);
}
