using System.Text;

namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// A data file of the DOS version (e.g. MAP_DATA.ALL or COMBAT.VGA). It starts with "ICS/ARTS" and contains named entries
/// like MAP_DATA.001 or COMBATBG.01, which are stored or packed.
///
/// Header: "ICS/ARTS", the entry count (word) and 21 bytes per entry: the file offset (dword), the method (byte),
/// the name (12 characters, padded with spaces), the unpacked size and the packed size (words, little endian).
/// The unpacked size is only the low word for entries of 64 KB and more.
/// </summary>
internal sealed class IcsArtsArchive
{
	public const string Signature = "ICS/ARTS";
	const int HeaderSize = 10;
	const int EntrySize = 21;
	const byte PackedMarker = 0x80;

	public enum Method : byte
	{
		Stored = 0,
		Packed = 1,
		// used for music, automaps and other data which is read as it is
		Raw = 2
	}

	public sealed record Entry(string Name, int Offset, Method Method, int Size, int PackedSize);

	readonly byte[] data;
	readonly Dictionary<string, Entry> entries = [];

	public IcsArtsArchive(byte[] data)
	{
		if (!IsArchive(data))
			throw new InvalidDataException("The data is no ICS/ARTS archive.");

		this.data = data;
		int count = data[8] | (data[9] << 8);

		for (int i = 0; i < count; i++)
		{
			var header = data.AsSpan(HeaderSize + i * EntrySize, EntrySize);
			int offset = header[0] | (header[1] << 8) | (header[2] << 16) | (header[3] << 24);
			var method = (Method)header[4];
			string name = Encoding.Latin1.GetString(header.Slice(5, 12)).TrimEnd(' ', '\0');
			int size = header[17] | (header[18] << 8);
			int packedSize = header[19] | (header[20] << 8);

			if (offset < 0 || offset + packedSize > data.Length)
				throw new InvalidDataException($"The entry {name} of the ICS/ARTS archive is outside of the data.");

			// some names exist twice (e.g. ICON_DAT.002), the first entry is used
			entries.TryAdd(name, new(name, offset, method, size, packedSize));
		}
	}

	public static bool IsArchive(ReadOnlySpan<byte> data) =>
		data.Length >= HeaderSize && data[..8].SequenceEqual(Encoding.ASCII.GetBytes(Signature));

	public IReadOnlyCollection<Entry> Entries => entries.Values;

	public bool Contains(string name) => entries.ContainsKey(name);

	/// <summary>
	/// The data of an entry. Packed entries are unpacked.
	/// </summary>
	public byte[] Read(string name)
	{
		if (!entries.TryGetValue(name, out var entry))
			throw new KeyNotFoundException($"The entry {name} does not exist in the ICS/ARTS archive.");

		var source = data.AsSpan(entry.Offset, entry.PackedSize);

		return entry.Method == Method.Packed ? Unpack(source, entry.Name) : source.ToArray();
	}

	/// <summary>
	/// Run length encoding of the DOS version. The data starts with 0x80, then each control byte is followed by its data:
	///
	/// - 0: end of data
	/// - 1 to 127: the next byte is repeated this many times
	/// - 128: one byte is skipped (the original keeps the previous content of the buffer, this gives 0)
	/// - 129 to 255: the given count minus 128 bytes follow as they are
	/// </summary>
	public static byte[] Unpack(ReadOnlySpan<byte> source, string name = "")
	{
		if (source.Length == 0 || source[0] != PackedMarker)
			throw new InvalidDataException($"The packed entry {name} does not start with 0x80.");

		var output = new List<byte>(source.Length * 2);
		int index = 1;

		while (true)
		{
			if (index >= source.Length)
				throw new InvalidDataException($"The packed entry {name} has no end marker.");

			int control = source[index++];

			if (control == 0)
				break;

			if (control < PackedMarker)
			{
				if (index >= source.Length)
					throw new InvalidDataException($"The packed entry {name} ends in a run.");

				byte value = source[index++];

				for (int i = 0; i < control; i++)
					output.Add(value);
			}
			else if (control == PackedMarker)
			{
				output.Add(0);
			}
			else
			{
				int count = control - PackedMarker;

				if (index + count > source.Length)
					throw new InvalidDataException($"The packed entry {name} ends in a literal block.");

				output.AddRange(source.Slice(index, count));
				index += count;
			}
		}

		return [.. output];
	}
}
