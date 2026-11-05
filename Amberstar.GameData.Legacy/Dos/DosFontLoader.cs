using Amber.Assets.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// The font of the DOS version (FONT.DAT) has 96 glyphs of 6x5 pixels with 4 bits per pixel, each followed by
/// 15 unused bytes. The first 64 glyphs are the characters from 32 to 95 (only capital letters, [, \, ] and ^
/// are Ä, Ü, Ö and ß), the last 32 glyphs are the runes.
/// The texts are converted to the characters of the Atari ST, so the tables map these characters.
/// </summary>
internal sealed class DosFontLoader(DosGameData dos) : IFontLoader
{
	const int GlyphCount = 96;
	const int GlyphDataSize = 30;
	const int RuneOffset = 64;
	Font? font;

	public IFont LoadFont()
	{
		if (font != null)
			return font;

		var data = dos.Read("AMBEDATA.DAT", "FONT.DAT");
		var glyphs = new Dictionary<int, IGraphic>(GlyphCount);

		for (int i = 0; i < GlyphCount && (i + 1) * GlyphDataSize <= data.Length; i++)
		{
			var pixels = DosGraphics.ChunkyToPixels(data.AsSpan(i * GlyphDataSize, 15), 6, 5);
			var rows = new byte[5];

			for (int y = 0; y < 5; y++)
			{
				for (int x = 0; x < 6; x++)
				{
					if (pixels[y * 6 + x] != 0)
						rows[y] |= (byte)(0x80 >> x);
				}
			}

			glyphs.Add(i, Graphic.FromBitPlanes(8, 5, rows, 1));
		}

		var textTable = new byte[224];
		var runeTable = new byte[224];

		for (int character = 32; character < 256; character++)
		{
			int glyph = TextGlyph(character);
			textTable[character - 32] = (byte)glyph;
			runeTable[character - 32] = (byte)(RuneLetter(character) is int letter ? RuneOffset + letter : glyph);
		}

		return font = new Font(textTable, runeTable, glyphs);
	}

	/// <summary>
	/// The glyph of a character of the Atari ST (0xff if there is none).
	/// </summary>
	static int TextGlyph(int character) => character switch
	{
		>= 0x20 and < 0x5b => character - 0x20,
		0x5f => 0x5f - 0x20,
		>= 0x61 and <= 0x7a => character - 0x40, // small letters use the capital letters
		0x8e or 0x84 => '[' - 0x20, // Ä ä
		0x9a or 0x81 => '\\' - 0x20, // Ü ü
		0x99 or 0x94 => ']' - 0x20, // Ö ö
		0x9e => '^' - 0x20, // ß
		_ => 0xff
	};

	/// <summary>
	/// The runes are the letters A to Z, Ä, Ü, Ö and ß (1 to 30).
	/// </summary>
	static int? RuneLetter(int character) => character switch
	{
		>= 'A' and <= 'Z' => character - 'A' + 1,
		>= 'a' and <= 'z' => character - 'a' + 1,
		0x8e or 0x84 => 27,
		0x9a or 0x81 => 28,
		0x99 or 0x94 => 29,
		0x9e => 30,
		_ => null
	};
}
