using Amber.Assets.Common;
namespace Amberstar.GameData.Legacy;

internal class Font(byte[] textConversionTable, byte[] runeConversionTable, Dictionary<int, IGraphic> glyphGraphics) : IFont
{
	public int Advance => 6;

	public int GlyphWidth => 8;

	public int GlyphHeight => 5;

	public int LineHeight => 7;

	const char SharpS = '\u009e';
	// The rune font of the Amiga versions has no sharp s, but a text of Firlas uses it. This is the rune of the Atari ST versions.
	static readonly IGraphic SharpSRune = Graphic.FromBitPlanes(8, 5, [0x18, 0x38, 0x58, 0x38, 0x18], 1);

	public IGraphic? GetGlyph(char character, bool rune)
	{
		if (character <= 32)
			return null;

		int index = character - 32;

		var mappingTable = rune ? runeConversionTable : textConversionTable;

		if (index >= mappingTable.Length)
			return null;

		index = mappingTable[index];

		if (index < 0)
			return null;

		var glyph = glyphGraphics.GetValueOrDefault(index);

		return glyph == null && rune && character == SharpS ? SharpSRune : glyph;
	}
}
