using System.IO.Compression;
using System.Text;
using Amber.IO.Common.FileSystem;

namespace Amberstar.Frontend;

/// <summary>
/// A version of the original game data (e.g. the English Amiga version).
/// </summary>
public sealed class GameVersion(string id, string language, string platform, Func<IReadOnlyFileSystem> loadFiles)
{
	/// <summary>
	/// Unique name inside the archive, e.g. "English-Amiga". It is also the folder of the savegames.
	/// </summary>
	public string Id { get; } = id;
	public string Language { get; } = language;
	public string Platform { get; } = platform;
	public string Name => $"{Language} ({Platform})";

	public IReadOnlyFileSystem LoadFiles() => loadFiles();
}

/// <summary>
/// Container for several game versions. It is a zip archive with a folder per version and the file
/// "versions.txt" which has a line "id;language;platform" per version (in the order of the selection).
///
/// The archive can be appended to the executable. Then a footer follows it: the archive size (8 bytes,
/// little endian) and the magic "AMBSDATA".
/// </summary>
public static class GameVersionArchive
{
	public const string FileName = "versions.dat";
	const string ManifestName = "versions.txt";
	static readonly byte[] FooterMagic = "AMBSDATA"u8.ToArray();
	const int FooterSize = 16;

	/// <summary>
	/// Only these files of a data folder are packed (the game reads nothing else).
	/// </summary>
	static readonly string[] DataFileExtensions = [".AMB", ".ICN", ".UDO", ".68K", ".SAV", ".IMG", ".TXT"];

	/// <summary>
	/// Returns the versions appended to the executable or the ones of the file versions.dat
	/// next to it. The list is empty if there are none.
	/// </summary>
	public static List<GameVersion> FindVersions()
	{
		var executablePath = Environment.ProcessPath;
		var versions = executablePath == null ? [] : LoadEmbedded(executablePath);

		if (versions.Count != 0)
			return versions;

		var path = Path.Combine(AppContext.BaseDirectory, FileName);

		return File.Exists(path) ? Load(File.ReadAllBytes(path)) : [];
	}

	/// <summary>
	/// Returns the versions appended to the given executable or an empty list.
	/// </summary>
	public static List<GameVersion> LoadEmbedded(string executablePath)
	{
		var data = ReadEmbeddedArchive(executablePath);

		return data == null ? [] : Load(data);
	}

	public static List<GameVersion> Load(byte[] archiveData)
	{
		using var archive = new ZipArchive(new MemoryStream(archiveData), ZipArchiveMode.Read);
		var manifest = archive.GetEntry(ManifestName) ?? throw new InvalidDataException($"The game data archive has no {ManifestName}.");
		using var reader = new StreamReader(manifest.Open());
		var versions = new List<GameVersion>();

		while (reader.ReadLine() is string line)
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			var parts = line.Split(';');

			if (parts.Length != 3)
				throw new InvalidDataException($"Invalid line in {ManifestName}: {line}");

			string id = parts[0];
			versions.Add(new GameVersion(id, parts[1], parts[2], () => LoadFiles(archiveData, id)));
		}

		return versions;
	}

	static IReadOnlyFileSystem LoadFiles(byte[] archiveData, string id)
	{
		using var archive = new ZipArchive(new MemoryStream(archiveData), ZipArchiveMode.Read);
		var fileSystem = new PackedFileSystem();
		string prefix = id + "/";

		foreach (var entry in archive.Entries)
		{
			if (!entry.FullName.StartsWith(prefix, StringComparison.Ordinal) || entry.Name.Length == 0)
				continue;

			var data = new byte[entry.Length];
			using var stream = entry.Open();
			stream.ReadExactly(data);
			fileSystem.AddFile(entry.FullName[prefix.Length..], data);
		}

		return fileSystem;
	}

	/// <summary>
	/// Packs the given data folders. Only the files of the folder itself are packed, not the sub folders.
	/// </summary>
	public static void Write(Stream output, IEnumerable<(string Id, string Language, string Platform, string Folder)> versions)
	{
		using var archive = new ZipArchive(output, ZipArchiveMode.Create, true);
		var manifest = new StringBuilder();

		foreach (var (id, language, platform, folder) in versions)
		{
			if (id.Contains(';') || language.Contains(';') || platform.Contains(';'))
				throw new ArgumentException("The names of a version must not contain ';'.");

			manifest.AppendLine($"{id};{language};{platform}");

			var files = Directory.GetFiles(folder)
				.Where(file => DataFileExtensions.Contains(Path.GetExtension(file).ToUpperInvariant()))
				.Order(StringComparer.Ordinal);

			foreach (var file in files)
				archive.CreateEntryFromFile(file, id + "/" + Path.GetFileName(file), CompressionLevel.SmallestSize);
		}

		using var writer = new StreamWriter(archive.CreateEntry(ManifestName).Open());
		writer.Write(manifest.ToString());
	}

	/// <summary>
	/// Appends the archive to a copy of the executable. Data which was appended before is replaced.
	/// </summary>
	public static void Embed(string executablePath, string archivePath, string outputPath)
	{
		var executable = File.ReadAllBytes(executablePath);
		var archiveData = File.ReadAllBytes(archivePath);
		long executableSize = executable.Length;

		if (TryReadFooter(executable, out long embeddedSize) && embeddedSize <= executableSize - FooterSize)
			executableSize -= embeddedSize + FooterSize;

		using var output = File.Create(outputPath);
		output.Write(executable, 0, (int)executableSize);
		output.Write(archiveData);
		output.Write(BitConverter.GetBytes((long)archiveData.Length));
		output.Write(FooterMagic);
	}

	static byte[]? ReadEmbeddedArchive(string executablePath)
	{
		using var file = File.OpenRead(executablePath);

		if (file.Length < FooterSize)
			return null;

		var footer = new byte[FooterSize];
		file.Seek(-FooterSize, SeekOrigin.End);
		file.ReadExactly(footer);

		if (!TryReadFooter(footer, out long size) || size > file.Length - FooterSize)
			return null;

		var data = new byte[size];
		file.Seek(-FooterSize - size, SeekOrigin.End);
		file.ReadExactly(data);

		return data;
	}

	static bool TryReadFooter(byte[] data, out long archiveSize)
	{
		archiveSize = 0;

		if (data.Length < FooterSize || !data.AsSpan(data.Length - FooterMagic.Length).SequenceEqual(FooterMagic))
			return false;

		archiveSize = BitConverter.ToInt64(data, data.Length - FooterSize);

		return archiveSize > 0;
	}
}
