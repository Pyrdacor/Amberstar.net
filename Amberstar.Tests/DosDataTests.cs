using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Legacy.Dos;
using Amberstar.GameData.Serialization;

namespace Amberstar.Tests;

public class DosDataTests
{
	public static TheoryData<string> DosFolders => new() { "German/AmberfilesDOS", "English/AmberfilesDOS" };

	static IEnumerable<string> GetArchives(string folder) =>
		Directory.GetFiles(TestData.GetDataPath(folder)).Where(file => IcsArtsArchive.IsArchive(File.ReadAllBytes(file)));

	[Theory]
	[MemberData(nameof(DosFolders))]
	public void AllEntriesCanBeRead(string folder)
	{
		var archives = GetArchives(folder).ToList();
		Assert.Equal(17, archives.Count);

		foreach (var file in archives)
		{
			var archive = new IcsArtsArchive(File.ReadAllBytes(file));

			foreach (var entry in archive.Entries)
			{
				var data = archive.Read(entry.Name);

				// the size in the header is only the low word
				Assert.True(data.Length % 0x10000 == entry.Size, $"{Path.GetFileName(file)}: {entry.Name} has {data.Length} bytes instead of {entry.Size}.");
			}
		}
	}

	[Fact]
	public void PackedDataIsUnpacked()
	{
		// a run of 3 x 0x11, 2 literal bytes, a skipped byte and the end
		byte[] packed = [0x80, 0x03, 0x11, 0x82, 0x22, 0x33, 0x80, 0x00];

		Assert.Equal(new byte[] { 0x11, 0x11, 0x11, 0x22, 0x33, 0x00 }, IcsArtsArchive.Unpack(packed));
	}

	[Fact]
	public void PackedDataMustStartWithMarker()
	{
		Assert.Throws<InvalidDataException>(() => IcsArtsArchive.Unpack([0x03, 0x11, 0x00]));
	}

	[Theory]
	[MemberData(nameof(DosFolders))]
	public void MapsHaveTheirNames(string folder)
	{
		var archive = new IcsArtsArchive(File.ReadAllBytes(Path.Combine(TestData.GetDataPath(folder), "MAP_DATA.ALL")));
		var map = archive.Read("MAP_DATA.001");

		// the name is a pascal string at offset 9
		Assert.Equal("LYRAMION", System.Text.Encoding.Latin1.GetString(map, 10, map[9]));
	}

	// The DOS data is compared with the Atari ST data of the same language. The DOS version (1.40) changed
	// a few things, so most but not all assets are equal.
	public static TheoryData<string, string> DosAndAtariFolders => new()
	{
		{ "German/AmberfilesDOS", "German/AmberfilesST" },
		{ "English/AmberfilesDOS", "English/AmberfilesST" },
	};

	[Theory]
	[MemberData(nameof(DosFolders))]
	public void TheDosVersionIsDetected(string folder)
	{
		Assert.Equal(LegacyPlatform.Dos, TestData.CreateAssetProvider(folder).Platform);
	}

	[Theory]
	[MemberData(nameof(DosAndAtariFolders))]
	public void MapsAreConverted(string dosFolder, string atariFolder)
	{
		var dos = TestData.CreateAssetProvider(dosFolder).MapLoader.LoadAllMaps();
		var atari = TestData.CreateAssetProvider(atariFolder).MapLoader.LoadAllMaps();
		Assert.Equal(atari.Count, dos.Count);
		int equalEvents = 0;

		foreach (var (index, map) in atari)
		{
			var dosMap = dos[index];
			Assert.Equal(map.Name, dosMap.Name);
			Assert.Equal(map.Type, dosMap.Type);
			Assert.Equal(map.Width, dosMap.Width);
			Assert.Equal(map.Height, dosMap.Height);

			if (map is IMap2D map2D)
			{
				var dos2D = Assert.IsAssignableFrom<IMap2D>(dosMap);
				Assert.Equal(map2D.TilesetIndex, dos2D.TilesetIndex);
				Assert.Equal(map2D.Tiles, dos2D.Tiles);
			}
			else
			{
				var map3D = Assert.IsAssignableFrom<IMap3D>(map);
				var dos3D = Assert.IsAssignableFrom<IMap3D>(dosMap);
				Assert.Equal(map3D.LabDataIndex, dos3D.LabDataIndex);
				// the German DOS version changed a tile of map 145
				int differentTiles = map3D.Tiles.Zip(dos3D.Tiles).Count(tiles => !tiles.First.Equals(tiles.Second));
				Assert.True(differentTiles <= 1, $"{differentTiles} tiles of map {index} differ.");
				Assert.Equal(map3D.LabTiles, dos3D.LabTiles);
			}

			if (map.Events.Select(e => e.ToString()).SequenceEqual(dosMap.Events.Select(e => e.ToString())))
				equalEvents++;

			Assert.Equal(map.CharacterPositions.Select(p => p.Length), dosMap.CharacterPositions.Select(p => p.Length));
		}

		Assert.True(equalEvents >= atari.Count - 5, $"Only {equalEvents} of {atari.Count} maps have the same events.");
	}

	[Theory]
	[MemberData(nameof(DosAndAtariFolders))]
	public void LabBlocksAreConverted(string dosFolder, string atariFolder)
	{
		var dos = TestData.CreateAssetProvider(dosFolder).LabDataLoader.LoadAllLabBlocks();
		var atari = TestData.CreateAssetProvider(atariFolder).LabDataLoader.LoadAllLabBlocks();
		Assert.Equal(atari.Count, dos.Count);

		foreach (var (index, block) in atari)
		{
			var dosBlock = dos[index];
			Assert.Equal(block.Type, dosBlock.Type);
			Assert.Equal(block.Perspectives.Length, dosBlock.Perspectives.Length);

			for (int i = 0; i < block.Perspectives.Length; i++)
			{
				var perspective = block.Perspectives[i];
				var dosPerspective = dosBlock.Perspectives[i];
				Assert.Equal(perspective.RenderPosition, dosPerspective.RenderPosition);
				Assert.Equal(perspective.SpecialRenderPosition, dosPerspective.SpecialRenderPosition);
				Assert.Equal(perspective.Frames.Length, dosPerspective.Frames.Length);

				for (int f = 0; f < perspective.Frames.Length; f++)
				{
					Assert.Equal(perspective.Frames[f].Width, dosPerspective.Frames[f].Width);
					Assert.Equal(perspective.Frames[f].GetData(), dosPerspective.Frames[f].GetData());
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(DosAndAtariFolders))]
	public void LabDataAndPalettesAreConverted(string dosFolder, string atariFolder)
	{
		var dos = TestData.CreateAssetProvider(dosFolder);
		var atari = TestData.CreateAssetProvider(atariFolder);

		for (int i = 1; i <= 23; i++)
		{
			var labData = atari.LabDataLoader.LoadLabData(i);
			var dosLabData = dos.LabDataLoader.LoadLabData(i);
			Assert.Equal(labData.CeilingIndex, dosLabData.CeilingIndex);
			Assert.Equal(labData.FloorIndex, dosLabData.FloorIndex);
			Assert.Equal(labData.PaletteIndex, dosLabData.PaletteIndex);
			Assert.Equal(labData.LabBlocks.Select(b => b.Index), dosLabData.LabBlocks.Select(b => b.Index));
		}

		for (int i = 1; i <= 10; i++)
		{
			// the DOS colors are finer, so only the 3 high bits are compared
			var colors = atari.PaletteLoader.LoadPalette(i).GetData();
			var dosColors = dos.PaletteLoader.LoadPalette(i).GetData();
			Assert.Equal(colors.Length, dosColors.Length);
			Assert.Equal(colors.Select(c => c & 0xe0), dosColors.Select(c => c & 0xe0));
		}
	}

	[Theory]
	[MemberData(nameof(DosAndAtariFolders))]
	public void ChestsAndPersonsAreConverted(string dosFolder, string atariFolder)
	{
		var dosProvider = TestData.CreateAssetProvider(dosFolder);
		var atariProvider = TestData.CreateAssetProvider(atariFolder);
		var dosChests = dosProvider.ChestLoader.LoadAllChests();

		foreach (var (index, chest) in atariProvider.ChestLoader.LoadAllChests())
			Assert.Equal(chest.Items.Select(item => item?.Index), dosChests[index].Items.Select(item => item?.Index));

		var dosPersons = dosProvider.PersonLoader.LoadAllPersons();

		foreach (var (index, person) in atariProvider.PersonLoader.LoadAllPersons())
		{
			var dosPerson = dosPersons[index];
			Assert.Equal(person.Name, dosPerson.Name);
			Assert.Equal(person.Type, dosPerson.Type);
			Assert.Equal(person.Class, dosPerson.Class);
			Assert.Equal(person.Level, dosPerson.Level);
			Assert.Equal(person.Inventory.Select(slot => slot.Item?.Index), dosPerson.Inventory.Select(slot => slot.Item?.Index));
			Assert.Equal(person.Portrait?.GetData(), dosPerson.Portrait?.GetData());
		}
	}

	[Theory]
	[MemberData(nameof(DosAndAtariFolders))]
	public void TextFragmentsAreConverted(string dosFolder, string atariFolder)
	{
		var dos = TestData.CreateAssetProvider(dosFolder).TextLoader;
		var atari = TestData.CreateAssetProvider(atariFolder).TextLoader;
		int equal = 0;
		int count = 0;

		static string? Word(ITextLoader loader, ushort index)
		{
			try
			{
				return loader.FromTextFragmentIndex(index).GetString();
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}
		}

		for (ushort i = 1; Word(atari, i) is string word && Word(dos, i) is string dosWord; i++)
		{
			count++;

			if (word == dosWord)
				equal++;
		}

		Assert.True(count > 1000);
		Assert.True(equal > count * 9 / 10, $"Only {equal} of {count} words are equal.");
	}

	sealed class SampleCollector : IMusicPlayer
	{
		public List<short> Samples { get; } = [];

		public void SampleData(short[] pcmData, bool endOfStream) => Samples.AddRange(pcmData);
	}

	[Theory]
	[MemberData(nameof(DosFolders))]
	public void SongsArePlayed(string folder)
	{
		var provider = TestData.CreateAssetProvider(folder);
		var songLoader = provider.SongLoader;
		Assert.Equal(19, songLoader.SongCount);
		Assert.Equal("CITYWALK", songLoader.SongNames[0]);
		Assert.Equal("HASTA LA VISTA", songLoader.SongNames[18]);

		for (int song = 1; song <= songLoader.SongCount; song++)
		{
			int musicIndex = songLoader.GetMusicIndex(song);
			var music = songLoader.LoadSong(musicIndex, 0);
			var collector = new SampleCollector();
			music.Play();

			// 20 seconds
			for (int i = 0; i < 20 * 60; i++)
				music.Update(1000.0 / 60, collector);

			var samples = collector.Samples;
			double rms = Math.Sqrt(samples.Average(s => (double)s * s));
			int clipped = samples.Count(s => s == short.MaxValue || s == short.MinValue);
			Assert.True(rms > 200, $"Song {song} is too quiet ({rms}).");
			Assert.True(clipped < samples.Count / 1000, $"Song {song} is clipped ({clipped} samples).");

			string? wavFolder = Environment.GetEnvironmentVariable("AMBERSTAR_WAV_FOLDER");

			if (wavFolder != null)
				WriteWav(Path.Combine(wavFolder, $"song{song:00}.wav"), samples);
		}
	}

	static void WriteWav(string path, List<short> samples)
	{
		using var writer = new BinaryWriter(File.Create(path));
		writer.Write("RIFF"u8);
		writer.Write(36 + samples.Count * 2);
		writer.Write("WAVEfmt "u8);
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)1);
		writer.Write(44100);
		writer.Write(44100 * 2);
		writer.Write((short)2);
		writer.Write((short)16);
		writer.Write("data"u8);
		writer.Write(samples.Count * 2);

		foreach (var sample in samples)
			writer.Write(sample);
	}

	[Theory]
	[MemberData(nameof(DosAndAtariFolders))]
	public void TilesetsAreConverted(string dosFolder, string atariFolder)
	{
		var dos = TestData.CreateAssetProvider(dosFolder).TilesetLoader;
		var atari = TestData.CreateAssetProvider(atariFolder).TilesetLoader;

		for (int i = 1; i <= 2; i++)
		{
			var tileset = atari.LoadTileset(i);
			var dosTileset = dos.LoadTileset(i);
			Assert.Equal(tileset.PlayerSpriteIndex, dosTileset.PlayerSpriteIndex);
			Assert.Equal(tileset.Tiles.Select(t => (t.ImageIndex, t.FrameCount, t.Flags)), dosTileset.Tiles.Select(t => (t.ImageIndex, t.FrameCount, t.Flags)));
			Assert.Equal(tileset.Graphics.Select(g => g.GetData()), dosTileset.Graphics.Select(g => g.GetData()));
		}
	}
}
