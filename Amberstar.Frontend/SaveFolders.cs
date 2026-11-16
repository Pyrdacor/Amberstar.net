namespace Amberstar.Frontend;

/// <summary>
/// Savegames and settings are stored in the folder Saves next to the executable. Without write permissions
/// there (e.g. in the program files) the folder in the user data is used. If there are no files next to the
/// executable but in the user data (e.g. from older versions which only used the user data), they are copied
/// next to the executable. If this fails the files in the user data are used.
/// </summary>
public sealed class SaveFolders(string executableFolder, string userFolder)
{
	public const string FolderName = "Saves";
	public const string SettingsFileName = "Settings.txt";
	public const string SavegameFileName = "Savegame.sav";
	public const string SavegameNamesFileName = "Savenames.txt";

	bool? executableFolderWritable;

	public SaveFolders()
		: this(Path.Combine(AppContext.BaseDirectory, FolderName),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Amberstar.net", FolderName))
	{
	}

	public string ExecutableFolder => executableFolder;

	public string UserFolder => userFolder;

	public string SettingsPath => Path.Combine(Choose("", GetSettingsFiles), SettingsFileName);

	/// <summary>
	/// The path of the savegame to continue the game. The slots are stored next to it.
	/// Each packed game version has its own sub folder.
	/// </summary>
	public string GetSavegamePath(string? versionId = null) =>
		Path.Combine(Choose(versionId ?? "", GetSavegameFiles), SavegameFileName);

	static string[] GetSettingsFiles(string folder)
	{
		var path = Path.Combine(folder, SettingsFileName);

		return File.Exists(path) ? [path] : [];
	}

	// the savegames and the file with their names (without the settings in the same folder)
	static string[] GetSavegameFiles(string folder)
	{
		if (!Directory.Exists(folder))
			return [];

		var savegames = Directory.GetFiles(folder, "Savegame*.sav");
		var names = Path.Combine(folder, SavegameNamesFileName);

		return savegames.Length != 0 && File.Exists(names) ? [.. savegames, names] : savegames;
	}

	string Choose(string subFolder, Func<string, string[]> getFiles)
	{
		var nextToExecutable = Path.Combine(executableFolder, subFolder);
		var inUserData = Path.Combine(userFolder, subFolder);

		if (!IsExecutableFolderWritable())
			return inUserData;

		if (getFiles(nextToExecutable).Length != 0)
			return nextToExecutable;

		var userFiles = getFiles(inUserData);

		if (userFiles.Length == 0)
			return nextToExecutable;

		try
		{
			Directory.CreateDirectory(nextToExecutable);

			// the files in the user data are kept
			foreach (var file in userFiles)
				File.Copy(file, Path.Combine(nextToExecutable, Path.GetFileName(file)), false);

			return nextToExecutable;
		}
		catch
		{
			return inUserData;
		}
	}

	/// <summary>
	/// Tries to write a file into the folder (or the folder above it if it doesn't exist yet).
	/// </summary>
	bool IsExecutableFolderWritable()
	{
		if (executableFolderWritable != null)
			return executableFolderWritable.Value;

		var folder = Directory.Exists(executableFolder) ? executableFolder : Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(executableFolder));

		try
		{
			if (folder == null || !Directory.Exists(folder))
				throw new DirectoryNotFoundException();

			using (File.Create(Path.Combine(folder, $".write-test-{Guid.NewGuid():N}"), 1, FileOptions.DeleteOnClose))
			{
			}

			executableFolderWritable = true;
		}
		catch
		{
			executableFolderWritable = false;
		}

		return executableFolderWritable.Value;
	}
}
