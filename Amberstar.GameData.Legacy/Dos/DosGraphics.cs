namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// Most graphics of the DOS version have 4 bits per pixel like the Atari ST graphics, but the pixels are stored
/// one after another (the high nibble is the left pixel) instead of in interleaved bitplanes.
/// </summary>
internal static class DosGraphics
{
	/// <summary>
	/// The pixels of 4 bit chunky graphic data. Each row has the given byte count.
	/// </summary>
	public static byte[] ChunkyToPixels(ReadOnlySpan<byte> data, int width, int height, int bytesPerRow)
	{
		var pixels = new byte[width * height];

		for (int y = 0; y < height; y++)
		{
			var row = data.Slice(y * bytesPerRow, bytesPerRow);

			for (int x = 0; x < width; x++)
			{
				byte value = row[x / 2];
				pixels[y * width + x] = (byte)(x % 2 == 0 ? value >> 4 : value & 0xf);
			}
		}

		return pixels;
	}

	public static byte[] ChunkyToPixels(ReadOnlySpan<byte> data, int width, int height) =>
		ChunkyToPixels(data, width, height, (width + 1) / 2);

	/// <summary>
	/// Converts pixels (color indices 0 to 15) to the interleaved bitplanes of the Atari ST.
	/// The width is filled up to a multiple of 16.
	/// </summary>
	public static byte[] PixelsToPlanar(ReadOnlySpan<byte> pixels, int width, int height, int planes = 4)
	{
		int groups = (width + 15) / 16;
		var data = new byte[groups * 2 * planes * height];
		int offset = 0;

		for (int y = 0; y < height; y++)
		{
			for (int group = 0; group < groups; group++)
			{
				for (int plane = 0; plane < planes; plane++)
				{
					int bits = 0;

					for (int x = 0; x < 16; x++)
					{
						int px = group * 16 + x;

						if (px < width && (pixels[y * width + px] & (1 << plane)) != 0)
							bits |= 0x8000 >> x;
					}

					data[offset++] = (byte)(bits >> 8);
					data[offset++] = (byte)bits;
				}
			}
		}

		return data;
	}

	public static byte[] ChunkyToPlanar(ReadOnlySpan<byte> data, int width, int height) =>
		PixelsToPlanar(ChunkyToPixels(data, width, height), width, height);

	/// <summary>
	/// Converts several graphics of the same size which follow each other.
	/// </summary>
	public static byte[] ChunkyToPlanar(ReadOnlySpan<byte> data, int width, int height, int count)
	{
		int size = (width + 1) / 2 * height;
		var result = new List<byte>(count * size);

		for (int i = 0; i < count && (i + 1) * size <= data.Length; i++)
			result.AddRange(ChunkyToPlanar(data.Slice(i * size, size), width, height));

		return [.. result];
	}

	/// <summary>
	/// The header of a graphic of the Atari ST (width - 1, height - 1 and the plane count) followed by the bitplanes.
	/// </summary>
	public static byte[] WithHeader(ReadOnlySpan<byte> pixels, int width, int height)
	{
		var planar = PixelsToPlanar(pixels, width, height);
		var data = new byte[6 + planar.Length];
		WriteWord(data, 0, width - 1);
		WriteWord(data, 2, height - 1);
		WriteWord(data, 4, 4);
		planar.CopyTo(data, 6);

		return data;
	}

	public static void WriteWord(byte[] data, int offset, int value)
	{
		data[offset] = (byte)(value >> 8);
		data[offset + 1] = (byte)value;
	}

	public static void WriteDword(byte[] data, int offset, long value)
	{
		data[offset] = (byte)(value >> 24);
		data[offset + 1] = (byte)(value >> 16);
		data[offset + 2] = (byte)(value >> 8);
		data[offset + 3] = (byte)value;
	}

	/// <summary>
	/// A palette of the DOS version (6 bit red, green and blue values) in the wide palette format of the Atari ST.
	/// The high bit of the color count marks the 6 bit values (the Atari ST has 3 bits per color component).
	/// </summary>
	public static byte[] VgaToWidePalette(ReadOnlySpan<byte> rgb, int colorCount = 16)
	{
		var data = new byte[2 + colorCount * 4];
		WriteWord(data, 0, 0x8000 | colorCount);

		for (int i = 0; i < colorCount; i++)
		{
			data[2 + i * 4 + 1] = rgb[i * 3];
			data[2 + i * 4 + 2] = rgb[i * 3 + 1];
			data[2 + i * 4 + 3] = rgb[i * 3 + 2];
		}

		return data;
	}
}
