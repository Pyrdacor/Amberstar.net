using Amberstar.Frontend;

// Packs the game data folders into the game version archive (versions.dat) and appends it to the executable.

// the default versions are the data folders of the repository, the first one is selected at the start
(string Id, string Language, string Platform, string Folder)[] defaultVersions =
[
	("English-Amiga", "English", "Amiga", Path.Combine("English", "AmberfilesAmiga")),
	("English-AtariST", "English", "Atari ST", Path.Combine("English", "AmberfilesST")),
	("German-Amiga", "Deutsch", "Amiga", Path.Combine("German", "AmberfilesAmiga")),
	("German-AtariST", "Deutsch", "Atari ST", Path.Combine("German", "AmberfilesST")),
];

try
{
	switch (args.FirstOrDefault()?.ToLowerInvariant())
	{
		case "pack" when args.Length == 2:
			Pack(args[1], defaultVersions.Select(version => version with { Folder = FindRepositoryFolder(version.Folder) }));
			return 0;
		case "pack" when args.Length > 2 && (args.Length - 2) % 4 == 0:
			Pack(args[1], args.Skip(2).Chunk(4).Select(version => (version[0], version[1], version[2], version[3])));
			return 0;
		case "embed" when args.Length == 3 || args.Length == 4:
			GameVersionArchive.Embed(args[1], args[2], args.Length == 4 ? args[3] : args[1]);
			Console.WriteLine($"Embedded {args[2]} into {(args.Length == 4 ? args[3] : args[1])}");
			return 0;
	}
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
{
	Console.WriteLine("Error: " + ex.Message);
	return 1;
}

Console.WriteLine("Usage:");
Console.WriteLine("  Amberstar.VersionPacker pack <archive>");
Console.WriteLine("      Packs the English and German Amiga and Atari ST data of the repository.");
Console.WriteLine("  Amberstar.VersionPacker pack <archive> (<id> <language> <platform> <folder>)...");
Console.WriteLine("      Packs the given data folders. The first version is selected at the start.");
Console.WriteLine("  Amberstar.VersionPacker embed <executable> <archive> [<output executable>]");
Console.WriteLine("      Appends the archive to the executable (replaces an embedded archive).");
Console.WriteLine();
Console.WriteLine($"Amberstar.net loads the archive from its executable or from the file {GameVersionArchive.FileName} next to it.");
return 1;

static void Pack(string archivePath, IEnumerable<(string Id, string Language, string Platform, string Folder)> versions)
{
	var versionList = versions.ToList();

	using (var output = File.Create(archivePath))
		GameVersionArchive.Write(output, versionList);

	foreach (var version in versionList)
		Console.WriteLine($"Packed {version.Language} ({version.Platform}) from {version.Folder}");

	Console.WriteLine($"Created {archivePath} ({new FileInfo(archivePath).Length / 1024} KB)");
}

// the data folders are searched upwards from the current directory
static string FindRepositoryFolder(string relativePath)
{
	var directory = new DirectoryInfo(Environment.CurrentDirectory);

	while (directory != null)
	{
		var path = Path.Combine(directory.FullName, relativePath);

		if (Directory.Exists(path))
			return path;

		directory = directory.Parent;
	}

	throw new DirectoryNotFoundException($"The data folder {relativePath} was not found.");
}
