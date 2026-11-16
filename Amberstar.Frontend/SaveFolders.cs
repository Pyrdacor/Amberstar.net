namespace Amberstar.Frontend;

/// <summary>
/// Savegames and settings are stored in the folder Saves next to the executable. Without write permissions
/// there (e.g. in the program files) the folder in the user data is used. Files in the user data are still
/// found when there are none next to the executable (e.g. from older versions which only used the user data).
/// </summary>
public sealed class SaveFolders(string executableFolder, string userFolder)
{
	public const string FolderName = "Saves";
	public const string SettingsFileName = "Settings.txt";
	public const string SavegameFileName = "Savegame.sav";

	bool? executableFolderWritable;

	public SaveFolders()
		: this(Path.Combine(AppContext.BaseDirectory, FolderName),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Amberstar.net", FolderName))
	{
	}

	public string ExecutableFolder => executableFolder;

	public string UserFolder => userFolder;

	public string SettingsPath => Path.Combine(Choose("", folder => File.Exists(Path.Combine(folder, SettingsFileName))), SettingsFileName);

	/// <summary>
	/// The path of the savegame to continue the game. The slots are stored next to it.
	/// Each packed game version has its own sub folder.
	/// </summary>
	public string GetSavegamePath(string? versionId = null) =>
		Path.Combine(Choose(versionId ?? "", ContainsSavegames), SavegameFileName);

	static bool ContainsSavegames(string folder) =>
		Directory.Exists(folder) && Directory.EnumerateFiles(folder, "Savegame*.sav").Any();

	string Choose(string subFolder, Func<string, bool> containsFiles)
	{
		var nextToExecutable = Path.Combine(executableFolder, subFolder);
		var inUserData = Path.Combine(userFolder, subFolder);
		bool writable = IsExecutableFolderWritable();

		if (writable && containsFiles(nextToExecutable))
			return nextToExecutable;

		if (containsFiles(inUserData))
			return inUserData;

		return writable ? nextToExecutable : inUserData;
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
