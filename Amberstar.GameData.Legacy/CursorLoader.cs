using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class CursorLoader(AssetProvider assetProvider) : ICursorLoader
{
	// the hotspot and 16x16 pixels with 4 bits
	const int DosCursorSize = 4 + 128;
	private readonly Dictionary<CursorType, ICursor> cursors = [];

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public bool OwnPalette => assetProvider.Platform == LegacyPlatform.Dos;

	public ICursor LoadCursor(CursorType cursorType)
	{
		if (cursors.TryGetValue(cursorType, out var cursor))
			return cursor;

		var asset = assetProvider.GetAsset(new(AssetType.Cursor, (int)cursorType));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Cursor {cursorType} not found.");

		var reader = asset.GetReader();

		int hotspotX = reader.ReadWord();
		int hotspotY = reader.ReadWord();
		Graphic graphic;

		if (reader.Size == DosCursorSize)
		{
			// The cursors of the DOS version have colors (4 bits per pixel, 0 is transparent).
			// The cursor sprite uses the color 14 as transparent color like the Atari ST, so 0 and 14 are swapped.
			var pixels = Dos.DosGraphics.ChunkyToPixels(reader.ReadBytes(128), 16, 16);

			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = pixels[i] switch { 0 => 14, 14 => 0, _ => pixels[i] };

			graphic = new Graphic(16, 16, pixels, GraphicFormat.PaletteIndices);
		}
		else
		{
			graphic = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(32), 1);
			var mask = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(32), 1);

			graphic.MaskWith(mask, 14);
			graphic.ReplaceColorIndex(1, 15);
		}

        cursor = new Cursor(new(hotspotX, hotspotY), graphic);

		cursors.Add(cursorType, cursor);

		return cursor;
	}
}
