using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class PaletteLoader(Amber.Assets.Common.IAssetProvider assetProvider, Dictionary<BuiltinPalette, IGraphic> builtinPalettes) : IPaletteLoader
{
	private readonly Dictionary<int, IGraphic> palettes = [];

	// Atari ST only had 3 bit per color channel (0-7).
	// So we have to map it to normal colors. 0 should map to 0 and 7 to 255.
	// A good approach is to multiply the value by 32 and add 16.
	// Also palettes store alpha as 0 while the color is still opaque.
	// Amiga seems to also use the same palette format.

	// The palettes of the DOS version have 6 bits per color component. They are marked by the high bit of the size.
	const int VgaPaletteFlag = 0x8000;

	public static IGraphic LoadWidePalette(IDataReader dataReader)
	{
		int size = dataReader.ReadWord();
		bool vga = (size & VgaPaletteFlag) != 0;
		size &= ~VgaPaletteFlag;
		var data = dataReader.ReadBytes(size * 4);
		int index = 0;

		byte ConvertColorComponent(byte c)
		{
			if (vga)
				return (byte)((c << 2) | (c >> 4));

			c *= 32;

			/*if (c == 224)
				c = 255;
			else if (c != 0)
				c += 16;*/

			return c;
		}

		for (int i = 0; i < size; i++)
		{
			data[index] = ConvertColorComponent(data[++index]); // R
			data[index] = ConvertColorComponent(data[++index]); // G
			data[index] = ConvertColorComponent(data[++index]); // B
			data[index++] = 0xff; // A
		}

		return Graphic.FromRGBA(size, 1, data);
	}

	public static byte[] LoadPaletteColors(IDataReader dataReader, int numColors)
	{
		var data = dataReader.ReadBytes(numColors * 2);

		// For compact palettes each color component is stored in a 4-bit nibble.
		// But still only 3 bits are used on the Atari. So we need to map it.
		// We achieve this by multiplying by 2 or left shifting by 1.
		for (int i = 0; i < numColors * 2; i++)
		{
			data[i] <<= 1;
		}

		return data;
	}

	/// <summary>
	/// Loads a compact palette of the Atari ST (16 colors with 2 bytes) or a wide palette of the DOS version.
	/// </summary>
	public static IGraphic LoadAnyPalette(IDataReader dataReader) =>
		(dataReader.PeekWord() & VgaPaletteFlag) != 0 ? LoadWidePalette(dataReader) : LoadPalette(dataReader);

	public static IGraphic LoadPalette(IDataReader dataReader)
	{
		var data = LoadPaletteColors(dataReader, 16);

		// Note: Graphic.FromPalette will handle alpha so we don't need to care about it here.
		return Graphic.FromPalette(data);
	}

	public IGraphic LoadPalette(int index)
	{
		if (palettes.TryGetValue(index, out var palette))
			return palette;

		var asset = assetProvider.GetAsset(new(AssetType.Palette, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Palette {index} not found.");

		palette = LoadWidePalette(asset.GetReader());

		palettes.Add(index, palette);

		return palette;
	}

    public IGraphic LoadBuiltinPalette(BuiltinPalette builtinPalette) => builtinPalettes[builtinPalette];
}
