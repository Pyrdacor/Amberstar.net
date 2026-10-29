using Amberstar.Frontend;
using Amberstar.GameData.Legacy;

namespace Amberstar.Tests;

/// <summary>
/// The game data can be packed into an archive which is appended to the executable.
/// </summary>
public class GameVersionTests
{
	static readonly (string Id, string Language, string Platform, string Folder, LegacyPlatform LegacyPlatform)[] Versions =
	[
		("English-Amiga", "English", "Amiga", Path.Combine("English", "AmberfilesAmiga"), LegacyPlatform.Amiga),
		("German-AtariST", "Deutsch", "Atari ST", Path.Combine("German", "AmberfilesST"), LegacyPlatform.Atari),
	];

	static byte[] PackVersions()
	{
		using var stream = new MemoryStream();
		GameVersionArchive.Write(stream, Versions.Select(version => (version.Id, version.Language, version.Platform, TestData.GetDataPath(version.Folder))));
		return stream.ToArray();
	}

	[Fact]
	public void PackedVersionsCanBeLoaded()
	{
		var versions = GameVersionArchive.Load(PackVersions());

		Assert.Equal(Versions.Select(version => version.Id), versions.Select(version => version.Id));
		Assert.Equal(["English (Amiga)", "Deutsch (Atari ST)"], versions.Select(version => version.Name));

		for (int i = 0; i < versions.Count; i++)
		{
			var assetProvider = new AssetProvider(versions[i].LoadFiles());

			Assert.Equal(Versions[i].LegacyPlatform, assetProvider.Platform);
			Assert.NotNull(assetProvider.SavegameLoader.LoadSavegame());
			// the same as with the data folder
			int folderMapCount = TestData.CreateAssetProvider(Versions[i].Folder).MapLoader.LoadAllMaps().Count;
			Assert.Equal(folderMapCount, assetProvider.MapLoader.LoadAllMaps().Count);
			Assert.NotNull(assetProvider.OutroData);
		}
	}

	[Fact]
	public void EmbeddedArchiveIsReplaced()
	{
		var folder = Directory.CreateTempSubdirectory("AmberstarTests");

		try
		{
			var executable = Path.Combine(folder.FullName, "game.exe");
			var archive = Path.Combine(folder.FullName, GameVersionArchive.FileName);
			byte[] executableData = [0x4d, 0x5a, 1, 2, 3];
			File.WriteAllBytes(executable, executableData);
			File.WriteAllBytes(archive, PackVersions());

			Assert.Empty(GameVersionArchive.LoadEmbedded(executable));

			GameVersionArchive.Embed(executable, archive, executable);
			long size = new FileInfo(executable).Length;
			GameVersionArchive.Embed(executable, archive, executable);

			Assert.Equal(size, new FileInfo(executable).Length);
			Assert.Equal(executableData, File.ReadAllBytes(executable)[..executableData.Length]);
			Assert.Equal(2, GameVersionArchive.LoadEmbedded(executable).Count);
		}
		finally
		{
			folder.Delete(true);
		}
	}
}
