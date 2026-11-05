namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// Converts the data of the DOS version to the data of the Atari ST version, so the same loaders read both.
///
/// The DOS version stores the same records, but words and longs are little endian, strings are pascal strings
/// (a length byte and the characters) and some arrays of records are stored as one array per field.
/// </summary>
internal static class DosConverter
{
	public const int ItemSize = 40;
	const int EventCount = 254;
	const int EventSize = 10;
	const int CharacterCount = 24;
	const int MaxLabTiles = 255;

	static int Word(ReadOnlySpan<byte> data, int offset) => data[offset] | (data[offset + 1] << 8);

	static void SwapWords(ReadOnlySpan<byte> source, int sourceOffset, byte[] target, int targetOffset, int wordCount)
	{
		for (int i = 0; i < wordCount; i++)
		{
			target[targetOffset + i * 2] = source[sourceOffset + i * 2 + 1];
			target[targetOffset + i * 2 + 1] = source[sourceOffset + i * 2];
		}
	}

	static void SwapDwords(ReadOnlySpan<byte> source, int sourceOffset, byte[] target, int targetOffset, int dwordCount)
	{
		for (int i = 0; i < dwordCount; i++)
		{
			for (int b = 0; b < 4; b++)
				target[targetOffset + i * 4 + b] = source[sourceOffset + i * 4 + 3 - b];
		}
	}

	/// <summary>
	/// Copies a pascal string into a field of the given size which is filled with the given byte.
	/// </summary>
	static void PascalString(ReadOnlySpan<byte> source, int sourceOffset, byte[] target, int targetOffset, int size, byte fill)
	{
		int length = Math.Min(source[sourceOffset], size);
		target.AsSpan(targetOffset, size).Fill(fill);
		source.Slice(sourceOffset + 1, length).CopyTo(target.AsSpan(targetOffset));
	}

	/// <summary>
	/// A text in the format of the Atari ST: the text count byte, a fill byte and words.
	/// </summary>
	public static byte[] Text(ReadOnlySpan<byte> data)
	{
		var result = data.ToArray();

		if (data.Length >= 2)
			SwapWords(data, 2, result, 2, (data.Length - 2) / 2);

		return result;
	}

	/// <summary>
	/// An array of little endian words (e.g. text indices of names) as big endian words.
	/// </summary>
	public static byte[] Words(ReadOnlySpan<byte> data)
	{
		var result = data.ToArray();
		SwapWords(data, 0, result, 0, data.Length / 2);

		return result;
	}

	/// <summary>
	/// Items have 40 bytes. The last 5 fields (classes, price, weight, index and name) are words.
	/// </summary>
	public static void Item(ReadOnlySpan<byte> source, int sourceOffset, byte[] target, int targetOffset)
	{
		source.Slice(sourceOffset, 30).CopyTo(target.AsSpan(targetOffset));
		SwapWords(source, sourceOffset + 30, target, targetOffset + 30, 5);
	}

	/// <summary>
	/// The items of chests and merchants.
	/// </summary>
	public static byte[] Items(ReadOnlySpan<byte> data)
	{
		var result = new byte[data.Length];

		for (int offset = 0; offset + ItemSize <= data.Length; offset += ItemSize)
			Item(data, offset, result, offset);

		return result;
	}

	/// <summary>
	/// A monster group has the 3 monster indices (words) and the 3 column masks.
	/// </summary>
	public static byte[] MonsterGroup(ReadOnlySpan<byte> data)
	{
		var result = data.ToArray();
		SwapWords(data, 0, result, 0, 3);

		return result;
	}

	/// <summary>
	/// The maps have the same header, but the events are stored as one array per field. The lab tiles of 3D maps
	/// use arrays for 255 lab tiles and the coordinates of the persons are 0-based.
	/// </summary>
	public static byte[] Map(ReadOnlySpan<byte> data)
	{
		var result = new List<byte>(data.Length + 2);
		int width = data[7];
		int height = data[8];
		int size = width * height;

		result.AddRange([data[1], data[0], data[3], data[2]]);
		result.AddRange(data.Slice(4, 5));

		var name = new byte[31];
		PascalString(data, 9, name, 0, 30, (byte)' ');
		name[30] = 0;
		result.AddRange(name);

		const int events = 40;

		for (int i = 0; i < EventCount; i++)
		{
			result.Add(data[events + i]);
			result.Add(data[events + EventCount + i]);
			result.Add(data[events + 2 * EventCount + i]);
			result.Add(data[events + 3 * EventCount + i]);
			result.Add(data[events + 4 * EventCount + i]);
			result.Add(data[events + 9 * EventCount + i]);
			result.Add(data[events + 5 * EventCount + 2 * i + 1]);
			result.Add(data[events + 5 * EventCount + 2 * i]);
			result.Add(data[events + 7 * EventCount + 2 * i + 1]);
			result.Add(data[events + 7 * EventCount + 2 * i]);
		}

		int offset = events + EventCount * EventSize;

		for (int i = 0; i < CharacterCount; i++)
			result.AddRange([data[offset + 2 * i + 1], data[offset + 2 * i]]);

		offset += 2 * CharacterCount;
		result.AddRange(data.Slice(offset, 5 * CharacterCount)); // icons, movement, flags, days and months
		offset += 5 * CharacterCount;
		result.AddRange([data[offset + 1], data[offset]]); // steps per day
		result.AddRange(data.Slice(offset + 2, 7));
		offset += 9;

		if (data[4] == 0) // 2D
		{
			result.AddRange(data.Slice(offset, 3 * size));
			offset += 3 * size;
		}
		else
		{
			int labTileCount = data[offset];
			result.Add((byte)labTileCount);
			int flags = offset + 1;

			// one array per byte of the flags (the lowest byte first)
			for (int i = 0; i < labTileCount; i++)
			{
				for (int b = 3; b >= 0; b--)
					result.Add(data[flags + b * MaxLabTiles + i]);
			}

			int blocks = flags + 4 * MaxLabTiles;

			for (int array = 0; array < 3; array++)
				result.AddRange(data.Slice(blocks + array * MaxLabTiles, labTileCount));

			offset = blocks + 3 * MaxLabTiles;
			result.AddRange(data.Slice(offset, 2 * size));
			offset += 2 * size;
		}

		// the coordinates of the persons are 0-based
		foreach (byte coordinate in data[offset..])
			result.Add((byte)(coordinate + 1));

		return [.. result];
	}

	/// <summary>
	/// The lab data has an array of 22 lab blocks. The image count and an unknown byte of the Atari ST
	/// version are not used.
	/// </summary>
	public static byte[] LabData(ReadOnlySpan<byte> data)
	{
		int count = data[0];
		var result = new List<byte>(count + 10) { 0, 0, (byte)count };
		result.AddRange(data.Slice(1, count));
		result.AddRange([0, 0, 2, data[24], data[25], 1, data[29]]);

		return [.. result];
	}

	/// <summary>
	/// The header of a lab block (LABBLOCK.nnn) has the type, the perspective count, the frame count,
	/// the render positions (1-based), the special render position and tables with the size, width and height
	/// of each image (17 perspectives with 7 frames each). The images (SPRBLOCK.nnn) follow each other with
	/// all perspectives of the first frame first. Each row has an even count of pixels.
	/// </summary>
	public static byte[] LabBlock(ReadOnlySpan<byte> header, ReadOnlySpan<byte> images)
	{
		const int Perspectives = 17;
		const int Frames = 7;
		int type = header[0];
		int perspectiveCount = header[1];
		int frameCount = header[2];
		bool isObject = type == 3;
		var result = new List<byte>(images.Length * 2) { 0, (byte)type, (byte)perspectiveCount, (byte)frameCount };

		static int Position(int value) => Math.Max(0, value - 1);

		void AddWord(int value) => result.AddRange([(byte)(value >> 8), (byte)value]);

		for (int i = 0; i < 2 * Perspectives; i++)
			AddWord(Position(Word(header, 6 + i * 2)));

		const int table = 74;

		if (isObject)
		{
			AddWord(Position(Word(header, table)));
			AddWord(Position(Word(header, table + 2)));
		}

		int sizes = table + 6;
		int widths = sizes + Perspectives * Frames * 2;
		int heights = widths + Perspectives * Frames * 2;
		int offset = 0;

		for (int frame = 0; frame < frameCount; frame++)
		{
			for (int perspective = 0; perspective < perspectiveCount; perspective++)
			{
				int index = (perspective * Frames + frame) * 2;
				int size = Word(header, sizes + index);
				int width = Word(header, widths + index);
				int height = Word(header, heights + index);
				var pixels = DosGraphics.ChunkyToPixels(images.Slice(offset, size), width, height);
				offset += size;

				var graphic = DosGraphics.WithHeader(pixels, width, height);
				var sizeData = new byte[4];
				DosGraphics.WriteDword(sizeData, 0, graphic.Length);
				result.AddRange(sizeData);
				result.AddRange(graphic);
			}
		}

		result.AddRange([0, 0, 0, 0]);

		return [.. result];
	}

	/// <summary>
	/// A background (BACKGRND.nnn) has the graphic count, 4 widths, 4 heights, 4 sizes and the total size.
	/// The graphics (SPR_GRND.nnn) have 1 byte per pixel and use the colors 16 to 31. Each graphic is followed
	/// by a second version which is not used. The colors from 160 are the sky.
	/// </summary>
	public static byte[] Background(ReadOnlySpan<byte> header, ReadOnlySpan<byte> images, byte skyColorIndex)
	{
		int count = header[0];
		var graphics = new List<byte[]>(count);
		int offset = 0;

		for (int i = 0; i < 4; i++)
		{
			int width = Word(header, 1 + i * 2);
			int height = Word(header, 9 + i * 2);
			int size = Word(header, 17 + i * 2);

			if (i % 2 == 0 && i / 2 < count)
			{
				var pixels = new byte[width * height];

				for (int p = 0; p < pixels.Length; p++)
				{
					byte value = images[offset + p];
					pixels[p] = value >= 160 ? skyColorIndex : (byte)(value & 0xf);
				}

				graphics.Add(DosGraphics.WithHeader(pixels, width, height));
			}

			offset += size;
		}

		var result = new List<byte>();
		var total = new byte[4];
		DosGraphics.WriteDword(total, 0, graphics.Sum(g => g.Length) + 1);
		result.AddRange(total);
		result.AddRange([(byte)graphics.Count, 0]);

		foreach (var graphic in graphics)
		{
			var size = new byte[4];
			DosGraphics.WriteDword(size, 0, graphic.Length);
			result.AddRange(size);
			result.AddRange(graphic);
		}

		return [.. result];
	}

	/// <summary>
	/// The tileset header (ICON_DAT.nnn) has the same fields, but little endian, and a VGA palette.
	/// The tiles (ICONS.nnn) have 16x16 pixels.
	/// </summary>
	public static byte[] Tileset(ReadOnlySpan<byte> header, ReadOnlySpan<byte> tiles)
	{
		const int TileCount = 250;
		var start = new byte[2 + 8 * TileCount];
		header[..(2 + TileCount)].CopyTo(start);
		SwapWords(header, 2 + TileCount, start, 2 + TileCount, TileCount);

		// the flags have one array per byte (the lowest byte first)
		for (int i = 0; i < TileCount; i++)
		{
			for (int b = 0; b < 4; b++)
				start[2 + 3 * TileCount + i * 4 + b] = header[2 + 3 * TileCount + (3 - b) * TileCount + i];
		}

		header.Slice(2 + 7 * TileCount, TileCount).CopyTo(start.AsSpan(2 + 7 * TileCount));
		var result = new List<byte>(start);

		int paletteOffset = 2 + 8 * TileCount;
		int colorCount = Word(header, paletteOffset);
		var rgb = new byte[colorCount * 3];

		for (int i = 0; i < colorCount; i++)
			header.Slice(paletteOffset + 2 + i * 4 + 1, 3).CopyTo(rgb.AsSpan(i * 3));

		result.AddRange(DosGraphics.VgaToWidePalette(rgb, colorCount));

		for (int offset = 0; offset + 128 <= tiles.Length; offset += 128)
			result.AddRange(DosGraphics.WithHeader(DosGraphics.ChunkyToPixels(tiles.Slice(offset, 128), 16, 16), 16, 16));

		return [.. result];
	}

	/// <summary>
	/// The monster graphics have a file for each row of the battle field (ROW001.nnn to ROW003.nnn).
	/// MONDATA.nnn gives the frame counts, widths, heights and sizes of the frames of each row.
	/// </summary>
	public static byte[] MonsterGraphics(ReadOnlySpan<byte> info, byte[][] rows)
	{
		var frames = new List<byte[]>();

		for (int row = 0; row < 3; row++)
		{
			int count = info[row];
			int width = Word(info, 3 + row * 2);
			int height = Word(info, 9 + row * 2);
			int size = width * height / 2;

			for (int i = 0; i < count && (i + 1) * size <= rows[row].Length; i++)
			{
				var planar = DosGraphics.ChunkyToPlanar(rows[row].AsSpan(i * size, size), width, height);
				var frame = new byte[4 + planar.Length];
				DosGraphics.WriteWord(frame, 0, (width + 15) / 16);
				DosGraphics.WriteWord(frame, 2, height);
				planar.CopyTo(frame, 4);
				frames.Add(frame);
			}
		}

		var result = new List<byte>();
		int offset = frames.Count * 4;

		foreach (var frame in frames)
		{
			var entry = new byte[4];
			DosGraphics.WriteDword(entry, 0, offset);
			result.AddRange(entry);
			offset += frame.Length;
		}

		foreach (var frame in frames)
			result.AddRange(frame);

		return [.. result];
	}

	/// <summary>
	/// The character records (CHARDATA.VGA) have a fixed size of 2289 bytes. They miss the first word of the
	/// Atari ST record, the name is a pascal string and the 21 items follow an unused item.
	/// The portrait has 4 bits per pixel. The texts of the conversations are stored in CODEDATA.CHA.
	/// </summary>
	public static byte[] Character(ReadOnlySpan<byte> data, byte[]? text)
	{
		const int StDataSize = 0x6aa;
		const int PortraitOffset = 0x6d1;
		var result = new byte[StDataSize];
		result[1] = 0xff;
		data[..0x44].CopyTo(result.AsSpan(2));
		SwapWords(data, 0x44, result, 0x46, 67);
		SwapDwords(data, 0xca, result, 0xcc, 9);
		PascalString(data, 0xee, result, 0xf0, 16, 0);
		data.Slice(0xff, 50).CopyTo(result.AsSpan(0x100));

		for (int i = 0; i < 21; i++)
			Item(data, 0x159 + i * ItemSize, result, 0x132 + i * ItemSize);

		data.Slice(0x4a1, 20).CopyTo(result.AsSpan(0x47a)); // interaction triggers
		SwapWords(data, 0x4b5, result, 0x48e, 20);
		data.Slice(0x4dd, 300).CopyTo(result.AsSpan(0x4b6)); // reactions
		SwapWords(data, 0x609, result, 0x5e2, 100);

		var portrait = data.Slice(PortraitOffset, 32 * 34 / 2);
		var output = new List<byte>(result);

		// a word 0 marks a missing portrait
		if (portrait.ContainsAnyExcept((byte)0))
			output.AddRange(DosGraphics.WithHeader(DosGraphics.ChunkyToPixels(portrait, 32, 34), 32, 34));
		else
			output.AddRange([0, 0]);

		if (text != null)
			output.AddRange(Text(text));
		else
			output.AddRange([0, 0, 0, 0]);

		return [.. output];
	}

	/// <summary>
	/// The class data (CLASSDAT.DAT) has one array per field: the maximum skill values, the experience for the
	/// levels (longs), the special values (words) and the spell schools of the 11 classes.
	/// </summary>
	public static List<byte[]> ClassData(ReadOnlySpan<byte> data)
	{
		const int Classes = 11;
		var result = new List<byte[]>(Classes);

		for (int c = 0; c < Classes; c++)
		{
			var record = new byte[Legacy.ClassData.DataSize];
			data.Slice(c * 10, 10).CopyTo(record);
			SwapDwords(data, Classes * 10 + c * 90 * 4, record, 10, 90);
			SwapWords(data, Classes * 10 + Classes * 90 * 4 + c * 11 * 2, record, 10 + 90 * 4, 11);
			record[10 + 90 * 4 + 11 * 2] = data[Classes * (10 + 90 * 4 + 11 * 2) + c];
			result.Add(record);
		}

		return result;
	}

	/// <summary>
	/// A place (PLACEDAT.nnn) has 12 words of data and a pascal string with the name.
	/// </summary>
	public static (byte[] Data, byte[] Name) Place(ReadOnlySpan<byte> data)
	{
		var name = new byte[30];
		PascalString(data, 24, name, 0, 30, (byte)' ');

		return (Words(data[..24]), name);
	}

	/// <summary>
	/// The first savegame (PARTYDAT.SAV) has the same fields as the one of the Atari ST, but little endian and
	/// with smaller arrays for the events, the chests and the gold of the chests. As it is only the savegame
	/// of a new game, the bits of the events and characters are all cleared.
	/// </summary>
	public static byte[] Savegame(ReadOnlySpan<byte> data)
	{
		const int TransportCount = 30;
		const int KnownWords = 1918;
		const int KnownWordsSize = 626;
		const int ChestSlots = KnownWords + KnownWordsSize;
		const int ChestSlotsSize = 272;
		const int ChestGold = ChestSlots + ChestSlotsSize;
		const int ChestGoldCount = 151;
		const int Wares = ChestGold + ChestGoldCount * 2;
		const int WaresSize = 1200;
		const int CombatPositions = Wares + WaresSize;
		const int TileChanges = CombatPositions + 6;

		const int StEvents = 226;
		const int StKnownWords = StEvents + 4064 + 1502;
		const int StChestSlots = StKnownWords + KnownWordsSize;
		const int StChestGold = StChestSlots + 1500;
		const int StWares = StChestGold + 2 * 1000;
		const int StCombatPositions = StWares + WaresSize;

		int tileChangeCount = Word(data, TileChanges);
		var result = new byte[StCombatPositions + 6 + 2 + tileChangeCount * 6];

		data[..24].CopyTo(result);
		SwapWords(data, 24, result, 24, 10); // year, map, party members, days and relative year
		data.Slice(44, 3 * TransportCount).CopyTo(result.AsSpan(44));
		SwapWords(data, 44 + 3 * TransportCount, result, 44 + 3 * TransportCount, TransportCount);
		data.Slice(194, 32).CopyTo(result.AsSpan(194)); // quests
		data.Slice(KnownWords, KnownWordsSize).CopyTo(result.AsSpan(StKnownWords));
		data.Slice(ChestSlots, ChestSlotsSize).CopyTo(result.AsSpan(StChestSlots));
		SwapWords(data, ChestGold, result, StChestGold, ChestGoldCount);
		data.Slice(Wares, WaresSize).CopyTo(result.AsSpan(StWares));
		data.Slice(CombatPositions, 6).CopyTo(result.AsSpan(StCombatPositions));
		result[StCombatPositions + 6] = (byte)(tileChangeCount >> 8);
		result[StCombatPositions + 7] = (byte)tileChangeCount;

		for (int i = 0; i < tileChangeCount; i++)
		{
			int source = TileChanges + 2 + i * 6;
			int target = StCombatPositions + 8 + i * 6;
			result[target] = data[source + 1];
			result[target + 1] = data[source];
			result[target + 2] = data[source + 2];
			result[target + 3] = data[source + 3];
			result[target + 4] = data[source + 5];
			result[target + 5] = data[source + 4];
		}

		return result;
	}
}
