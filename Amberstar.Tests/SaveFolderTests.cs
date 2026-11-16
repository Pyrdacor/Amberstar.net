using Amberstar.Frontend;

namespace Amberstar.Tests;

public sealed class SaveFolderTests : IDisposable
{
	readonly string root = Path.Combine(Path.GetTempPath(), $"Amberstar.net-SaveFolders-{Guid.NewGuid():N}");
	readonly string executableFolder;
	readonly string userFolder;

	public SaveFolderTests()
	{
		executableFolder = Path.Combine(root, "Game", SaveFolders.FolderName);
		userFolder = Path.Combine(root, "User", SaveFolders.FolderName);
		Directory.CreateDirectory(Path.Combine(root, "Game"));
	}

	public void Dispose()
	{
		if (Directory.Exists(root))
			Directory.Delete(root, true);
	}

	static void CreateFile(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllBytes(path, [1]);
	}

	[Fact]
	public void SavegamesAreNextToTheExecutableByDefault()
	{
		var folders = new SaveFolders(executableFolder, userFolder);

		Assert.Equal(Path.Combine(executableFolder, "Savegame.sav"), folders.GetSavegamePath());
		Assert.Equal(Path.Combine(executableFolder, "AmigaGerman", "Savegame.sav"), folders.GetSavegamePath("AmigaGerman"));
		Assert.Equal(Path.Combine(executableFolder, "Settings.txt"), folders.SettingsPath);
	}

	[Fact]
	public void SavegamesAndSettingsInTheUserDataAreCopiedNextToTheExecutable()
	{
		CreateFile(Path.Combine(userFolder, "AmigaGerman", "Savegame.sav"));
		CreateFile(Path.Combine(userFolder, "AmigaGerman", "Savegame_03.sav"));
		CreateFile(Path.Combine(userFolder, "AmigaGerman", "Savenames.txt"));
		CreateFile(Path.Combine(userFolder, "Settings.txt"));
		var folders = new SaveFolders(executableFolder, userFolder);

		Assert.Equal(Path.Combine(executableFolder, "AmigaGerman", "Savegame.sav"), folders.GetSavegamePath("AmigaGerman"));
		Assert.Equal(Path.Combine(executableFolder, "Settings.txt"), folders.SettingsPath);
		Assert.Equal(["Savegame.sav", "Savegame_03.sav", "Savenames.txt"],
			Directory.GetFiles(Path.Combine(executableFolder, "AmigaGerman")).Select(Path.GetFileName).Order(StringComparer.Ordinal));
		Assert.True(File.Exists(Path.Combine(executableFolder, "Settings.txt")));
		// the files in the user data are kept
		Assert.True(File.Exists(Path.Combine(userFolder, "AmigaGerman", "Savegame_03.sav")));
		// other versions without savegames in the user data use the folder next to the executable
		Assert.Equal(Path.Combine(executableFolder, "AtariGerman", "Savegame.sav"), folders.GetSavegamePath("AtariGerman"));
		Assert.False(Directory.Exists(Path.Combine(executableFolder, "AtariGerman")));
	}

	[Fact]
	public void TheSettingsAloneAreNoSavegames()
	{
		// without packed versions the savegames are in the same folder as the settings
		CreateFile(Path.Combine(userFolder, "Settings.txt"));
		var folders = new SaveFolders(executableFolder, userFolder);

		Assert.Equal(Path.Combine(executableFolder, "Savegame.sav"), folders.GetSavegamePath());
		Assert.Equal(Path.Combine(executableFolder, "Settings.txt"), folders.SettingsPath);
		Assert.Equal(["Settings.txt"], Directory.GetFiles(executableFolder).Select(Path.GetFileName));
	}

	[Fact]
	public void SavegamesNextToTheExecutableComeFirst()
	{
		CreateFile(Path.Combine(userFolder, "AmigaGerman", "Savegame.sav"));
		CreateFile(Path.Combine(executableFolder, "AmigaGerman", "Savegame.sav"));
		var folders = new SaveFolders(executableFolder, userFolder);

		Assert.Equal(Path.Combine(executableFolder, "AmigaGerman", "Savegame.sav"), folders.GetSavegamePath("AmigaGerman"));
	}

	[Fact]
	public void WithoutWritePermissionsTheUserDataIsUsed()
	{
		// a folder which can't be created (its parent is a file)
		CreateFile(Path.Combine(root, "File"));
		var folders = new SaveFolders(Path.Combine(root, "File", SaveFolders.FolderName), userFolder);

		Assert.Equal(Path.Combine(userFolder, "AmigaGerman", "Savegame.sav"), folders.GetSavegamePath("AmigaGerman"));
		Assert.Equal(Path.Combine(userFolder, "Settings.txt"), folders.SettingsPath);
	}
}
