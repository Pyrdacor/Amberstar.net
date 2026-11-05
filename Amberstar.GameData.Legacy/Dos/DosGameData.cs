using Amber.Common;
using Amber.IO.Common.FileSystem;

namespace Amberstar.GameData.Legacy.Dos;

/// <summary>
/// The game data of the DOS version. The data files are ICS/ARTS archives (see <see cref="IcsArtsArchive"/>).
/// The assets are converted to the format of the Atari ST version (see <see cref="DosConverter"/>).
/// </summary>
internal sealed class DosGameData
{
	public const string ProgramFileName = "GAME.EXE";
	public const int CharacterRecordSize = 2289;
	readonly IReadOnlyFileSystem fileSystem;
	readonly Dictionary<string, IcsArtsArchive> archives = [];
	readonly Dictionary<AssetType, Dictionary<int, byte[]>> assets = [];
	byte[]? program;
	int dataSegmentOffset = -1;

	public DosGameData(IReadOnlyFileSystem fileSystem)
	{
		this.fileSystem = fileSystem;
		German = fileSystem.GetFile("DICTIONA.GER") != null;
	}

	/// <summary>
	/// The DOS version has the program GAME.EXE and the archive MAP_DATA.ALL.
	/// </summary>
	public static bool IsDosVersion(IReadOnlyFileSystem fileSystem) =>
		fileSystem.GetFile(ProgramFileName) != null && fileSystem.GetFile("MAP_DATA.ALL") != null;

	public bool German { get; }

	public byte[] ReadFile(string name)
	{
		var file = fileSystem.GetFile(name) ?? throw new FileNotFoundException($"The file {name} of the DOS version is missing.");
		using var reader = file.Stream.GetReader();
		reader.Position = 0;

		return reader.ReadBytes(reader.Size);
	}

	public IcsArtsArchive Archive(string name)
	{
		if (!archives.TryGetValue(name, out var archive))
		{
			archive = new IcsArtsArchive(ReadFile(name));
			archives.Add(name, archive);
		}

		return archive;
	}

	public byte[] Read(string archive, string entry) => Archive(archive).Read(entry);

	public byte[]? TryRead(string archive, string entry) =>
		fileSystem.GetFile(archive) != null && Archive(archive).Contains(entry) ? Archive(archive).Read(entry) : null;

	/// <summary>
	/// The numbered entries of an archive (e.g. MAP_DATA.001), by their number.
	/// </summary>
	IEnumerable<(int Index, string Name)> Numbered(string archive, string prefix)
	{
		foreach (var entry in Archive(archive).Entries)
		{
			if (!entry.Name.StartsWith(prefix, StringComparison.Ordinal))
				continue;

			// e.g. MAP_DATA.001 or CODE0001.MAP
			var number = new string([.. entry.Name[prefix.Length..].TakeWhile(char.IsAsciiDigit)]);

			if (int.TryParse(number, out int index))
				yield return (index, entry.Name);
		}
	}

	#region Program

	public byte[] Program => program ??= ReadFile(ProgramFileName);

	/// <summary>
	/// The file offset of the data segment of the Turbo Pascal program. The program starts with a call of the
	/// initialization of the system unit, which loads the data segment (mov dx,segment; mov ds,dx).
	/// </summary>
	public int DataSegmentOffset
	{
		get
		{
			if (dataSegmentOffset != -1)
				return dataSegmentOffset;

			var data = Program;
			int headerSize = (data[8] | (data[9] << 8)) * 16;
			int entry = headerSize + (data[0x16] | (data[0x17] << 8)) * 16 + (data[0x14] | (data[0x15] << 8));

			if (data[entry] != 0x9a) // call far
				throw new InvalidDataException("The start of GAME.EXE is unknown.");

			int systemInit = headerSize + (data[entry + 3] | (data[entry + 4] << 8)) * 16 + (data[entry + 1] | (data[entry + 2] << 8));

			if (data[systemInit] != 0xba || data[systemInit + 3] != 0x8e || data[systemInit + 4] != 0xda)
				throw new InvalidDataException("The data segment of GAME.EXE was not found.");

			return dataSegmentOffset = headerSize + (data[systemInit + 1] | (data[systemInit + 2] << 8)) * 16;
		}
	}

	public ReadOnlySpan<byte> DataSegment(int offset, int length) => Program.AsSpan(DataSegmentOffset + offset, length);

	/// <summary>
	/// A pascal string of the data segment.
	/// </summary>
	public byte[] DataSegmentString(int offset) => DataSegment(offset + 1, Program[DataSegmentOffset + offset]).ToArray();

	#endregion

	#region Assets

	public ICollection<int> GetAssetKeys(AssetType type) => GetAssets(type)?.Keys ?? (ICollection<int>)[];

	public byte[]? GetAsset(AssetType type, int index) => GetAssets(type)?.GetValueOrDefault(index);

	public bool HasAssetType(AssetType type) => GetAssets(type) != null;

	Dictionary<int, byte[]>? GetAssets(AssetType type)
	{
		if (assets.TryGetValue(type, out var result))
			return result;

		result = LoadAssets(type);

		if (result != null)
			assets.Add(type, result);

		return result;
	}

	Dictionary<int, byte[]>? LoadAssets(AssetType type)
	{
		Dictionary<int, byte[]> Convert(string archive, string prefix, Func<byte[], byte[]> converter) =>
			Numbered(archive, prefix).ToDictionary(e => e.Index, e => converter(Read(archive, e.Name)));

		switch (type)
		{
			case AssetType.Map:
				return Convert("MAP_DATA.ALL", "MAP_DATA.", data => DosConverter.Map(data));
			case AssetType.MapText:
				return Numbered("CODEDATA.MAP", "CODE").ToDictionary(e => e.Index, e => DosConverter.Text(Read("CODEDATA.MAP", e.Name)));
			case AssetType.ItemText:
				return new()
				{
					{ 1, DosConverter.Text(Read("AMBEDATA.DAT", "CODE0001.TXT")) },
					{ 2, DosConverter.Text(Read("AMBEDATA.DAT", "CODE0002.TXT")) },
				};
			case AssetType.PuzzleText:
				return new() { { 1, DosConverter.Text(Read("AMBEDATA.DAT", "CODE0000.PUZ")) } };
			case AssetType.PuzzleGraphics:
			{
				var data = Read("ICN_DATA.VGA", "PUZZLE.ICN");
				var result = new List<byte>();
				result.AddRange(DosGraphics.ChunkyToPlanar(data.AsSpan(0, 176 * 144 / 2), 176, 144));
				result.AddRange(DosGraphics.ChunkyToPlanar(data.AsSpan(176 * 144 / 2, 96 * 96 / 2), 96, 96));
				result.AddRange(DosGraphics.ChunkyToPlanar(data.AsSpan(176 * 144 / 2 + 96 * 96 / 2), 32, 32, 2 * IAltarPuzzle.PieceCount));

				return new() { { 1, [.. result] } };
			}
			case AssetType.CombatBackground:
				return Convert("COMBAT.VGA", "COMBATBG.", data => DosGraphics.ChunkyToPlanar(data.AsSpan(48), 176, 112));
			case AssetType.MonsterGraphics:
				return Numbered("COMBAT.VGA", "MONDATA.").ToDictionary(e => e.Index, e => DosConverter.MonsterGraphics(Read("COMBAT.VGA", e.Name),
					[.. Enumerable.Range(1, 3).Select(row => Read("COMBAT.VGA", $"ROW{row:000}.{e.Index:000}"))]));
			case AssetType.TacticalIcons:
				return new() { { 1, IconList(Read("ICN_DATA.VGA", "TAKTIK.ICN")) } };
			case AssetType.SpellAnimations:
				return new() { { 1, IconList(Read("ICN_DATA.VGA", "F_T_ANIM.ICN")) } };
			case AssetType.Palette:
				return Numbered("AMBEDATA.DAT", "COL_PALL.").Where(e => e.Index < 100)
					.ToDictionary(e => e.Index, e => DosGraphics.VgaToWidePalette(Read("AMBEDATA.DAT", e.Name)));
			case AssetType.Graphics80x80:
			{
				// the odd assets are the graphics, the even ones their palettes (the VGA palette is stored first)
				var result = new Dictionary<int, byte[]>();

				foreach (var (index, name) in Numbered("PICTURES.VGA", "MINI_PIC."))
				{
					var data = Read("PICTURES.VGA", name);
					result.Add(index * 2 - 1, DosGraphics.ChunkyToPlanar(data.AsSpan(48), 80, 80));
					result.Add(index * 2, DosGraphics.VgaToWidePalette(data));
				}

				return result;
			}
			case AssetType.Tileset:
				return Numbered("ICON_DAT.VGA", "ICON_DAT.").DistinctBy(e => e.Index).ToDictionary(e => e.Index,
					e => DosConverter.Tileset(Read("ICON_DAT.VGA", e.Name), Read("ICON_DAT.VGA", $"ICONS.{e.Index:000}")));
			case AssetType.Savegame:
				return new() { { 1, DosConverter.Savegame(ReadFile("PARTYDAT.SAV")) } };
			case AssetType.LabData:
				return Convert("AMBEDATA.DAT", "LAB_DATA.", data => DosConverter.LabData(data));
			case AssetType.LabBlock:
				return Numbered("LABBLOCK.VGA", "LABBLOCK.").ToDictionary(e => e.Index,
					e => DosConverter.LabBlock(Read("LABBLOCK.VGA", e.Name), Read("LABBLOCK.VGA", $"SPRBLOCK.{e.Index:000}")));
			case AssetType.Background:
				return Numbered("BACKGRND.VGA", "BACKGRND.").ToDictionary(e => e.Index,
					e => DosConverter.Background(Read("BACKGRND.VGA", e.Name), Read("BACKGRND.VGA", $"SPR_GRND.{e.Index:000}"), SkyColorIndex));
			case AssetType.MonsterGroup:
				return Convert("COMBAT.VGA", "MON_DATA.", data => DosConverter.MonsterGroup(data));
			case AssetType.Person:
			{
				var data = ReadFile("CHARDATA.VGA");
				var texts = Archive("CODEDATA.CHA");
				var result = new Dictionary<int, byte[]>();

				for (int i = 0; i < data.Length / CharacterRecordSize; i++)
				{
					string textName = $"CODE{i + 1:0000}.CHA";
					var text = texts.Contains(textName) ? texts.Read(textName) : null;
					result.Add(i + 1, DosConverter.Character(data.AsSpan(i * CharacterRecordSize, CharacterRecordSize), text));
				}

				return result;
			}
			case AssetType.Chest:
				return Convert("CHESTDAT.ALL", "CHESTDAT.", data => DosConverter.Items(data));
			case AssetType.Merchant:
				return Convert("WARESDAT.ALL", "WARESDAT.", data => DosConverter.Items(data));
			default:
				return null; // program data
		}
	}

	/// <summary>
	/// The color which the sky of the backgrounds uses in the Atari ST version.
	/// </summary>
	public const byte SkyColorIndex = 11;

	static byte[] IconList(byte[] data) => DosGraphics.ChunkyToPlanar(data, 16, 16, data.Length / 128);

	#endregion
}
