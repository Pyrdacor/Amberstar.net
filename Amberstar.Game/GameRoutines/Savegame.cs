using Amber.Common;
using Amber.Serialization;

namespace Amberstar.Game;

/// <summary>
/// A savegame in one of the slots.
/// </summary>
internal sealed record SavegameSlot(int Slot, string Path, string Name, DateTime SaveTime);

/// <summary>
/// Besides the savegame to continue the game there are numbered slots (the original has one savegame).
/// A savegame contains the party data
/// (PARTYDAT.SAV), the character records of the party members and dismissed
/// characters (CHARDATA.AMB)
/// and the automaps (AUTOMAP.AMB) which the original stores on the disk.
/// </summary>
partial class Game
{
	const dword SavegameMagic = 0x414d4253; // "AMBS"
	const byte SavegameVersion = 1;
	internal const int SavegameSlotCount = 30;
	internal const int MaxSavegameNameLength = 20;
	const string SavegameNamesFileName = "Savenames.txt";

	/// <summary>
	/// Path of the savegame file to continue the game. It is set by the frontend.
	/// The slots are stored next to it.
	/// </summary>
	public string? SavegamePath { get; set; }

	public bool HasSavegame => SavegamePath != null && File.Exists(SavegamePath);

	/// <summary>
	/// Gets the path of a savegame slot (1 to <see cref="SavegameSlotCount"/>).
	/// </summary>
	internal string GetSavegameSlotPath(int slot)
	{
		if (SavegamePath == null)
			throw new InvalidOperationException("No savegame path is set.");

		return Path.Combine(Path.GetDirectoryName(SavegamePath) ?? "", $"Savegame_{slot:00}.sav");
	}

	/// <summary>
	/// The used savegame slots.
	/// </summary>
	internal List<SavegameSlot> GetSavegameSlots()
	{
		var slots = new List<SavegameSlot>();

		if (SavegamePath == null)
			return slots;

		var names = LoadSavegameNames();

		for (int slot = 1; slot <= SavegameSlotCount; slot++)
		{
			var path = GetSavegameSlotPath(slot);

			if (File.Exists(path))
				slots.Add(new(slot, path, GetSavegameName(names, slot), File.GetLastWriteTime(path)));
		}

		return slots;
	}

	/// <summary>
	/// Letters and numbers of English and German, a few special characters and only what the font can show.
	/// </summary>
	internal bool IsValidSavegameNameCharacter(char ch) =>
		(ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or
		'\u00c4' or '\u00d6' or '\u00dc' or '\u00e4' or '\u00f6' or '\u00fc' or '\u00df' or
		' ' or '_' or '-' or '+' or '*' or '?' or '!' or '%') && TextManager.HasGlyph(ch);

	string NormalizeSavegameName(string name)
	{
		name = new string(name.Where(IsValidSavegameNameCharacter).ToArray()).Trim();

		return name.Length > MaxSavegameNameLength ? name[..MaxSavegameNameLength].TrimEnd() : name;
	}

	string? SavegameNamesPath => SavegamePath == null ? null : Path.Combine(Path.GetDirectoryName(SavegamePath) ?? "", SavegameNamesFileName);

	/// <summary>
	/// Reads the names of the savegame slots. The file has a line like 01=NAME for each named slot.
	/// </summary>
	internal Dictionary<int, string> LoadSavegameNames()
	{
		var names = new Dictionary<int, string>();
		var path = SavegameNamesPath;

		if (path == null || !File.Exists(path))
			return names;

		foreach (var line in File.ReadAllLines(path, System.Text.Encoding.UTF8))
		{
			int separator = line.IndexOf('=');

			if (separator != -1 && int.TryParse(line[..separator], out int slot) && slot >= 1 && slot <= SavegameSlotCount)
			{
				var name = NormalizeSavegameName(line[(separator + 1)..]);

				if (name.Length != 0)
					names[slot] = name;
			}
		}

		return names;
	}

	internal static string GetSavegameName(Dictionary<int, string> names, int slot) =>
		names.TryGetValue(slot, out var name) ? name : $"Save {slot:00}";

	/// <summary>
	/// Saves the game to a slot with the given name. It also is the savegame to continue the game then.
	/// Without a name nothing is saved.
	/// </summary>
	internal bool SaveGameToSlot(int slot, string name)
	{
		name = NormalizeSavegameName(name);

		if (name.Length == 0)
			return false;

		var path = GetSavegameSlotPath(slot);
		SaveGame(path);
		File.Copy(path, SavegamePath!, true);

		var names = LoadSavegameNames();
		names[slot] = name;
		File.WriteAllLines(SavegameNamesPath!, names.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key:00}={entry.Value}"), new System.Text.UTF8Encoding(false));

		return true;
	}

	/// <summary>
	/// Saves the game to the given file or the savegame to continue the game.
	/// </summary>
	public void SaveGame(string? path = null)
	{
		path ??= SavegamePath;

		if (path == null)
			throw new InvalidOperationException("No savegame path is set.");

		var writer = new DataWriter();
		writer.Write(SavegameMagic);
		writer.Write(SavegameVersion);

		var savegameWriter = new DataWriter();
		State.ToSavegame(AssetProvider.SavegameLoader).Write(savegameWriter, false);
		var savegameData = savegameWriter.ToArray();
		writer.Write((dword)savegameData.Length);
		writer.Write(savegameData);

		var partyMembers = State.SavedCharactersWithCharacterIndex.ToList();
		writer.Write((word)partyMembers.Count);

		foreach (var (characterIndex, partyMember) in partyMembers)
		{
			var record = AssetProvider.PersonLoader.WritePartyMember(partyMember);
			writer.Write((word)characterIndex);
			writer.Write((word)record.Length);
			writer.Write(record);
		}

		var automaps = State.Automaps;
		writer.Write((word)automaps.Count);

		foreach (var (mapIndex, automap) in automaps)
		{
			writer.Write((word)mapIndex);
			writer.Write((word)automap.Length);
			writer.Write(automap);
		}

		var directory = Path.GetDirectoryName(path);

		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		File.WriteAllBytes(path, writer.ToArray());
	}

	/// <summary>
	/// Loads the given file or the savegame to continue the game.
	/// </summary>
	public void LoadGame(string? path = null)
	{
		path ??= SavegamePath;

		if (path == null || !File.Exists(path))
			throw new InvalidOperationException("There is no savegame.");

		var reader = new DataReader(File.ReadAllBytes(path));

		if (reader.ReadDword() != SavegameMagic)
			throw new InvalidDataException("The file is no savegame.");

		if (reader.ReadByte() > SavegameVersion)
			throw new InvalidDataException("The savegame was written by a newer version.");

		var savegame = AssetProvider.SavegameLoader.Create();
		savegame.Read(new DataReader(reader.ReadBytes((int)reader.ReadDword())));

		State.LoadFrom(savegame);
		State.ResetPartyMembers();

		int partyMemberCount = reader.ReadWord();

		for (int i = 0; i < partyMemberCount; i++)
		{
			int characterIndex = reader.ReadWord();
			var record = reader.ReadBytes(reader.ReadWord());
			State.ReplacePartyMember(characterIndex, AssetProvider.PersonLoader.ReadPartyMember(characterIndex, record));
		}

		var automaps = new Dictionary<int, byte[]>();
		int automapCount = reader.ReadWord();

		for (int i = 0; i < automapCount; i++)
		{
			int mapIndex = reader.ReadWord();
			automaps[mapIndex] = reader.ReadBytes(reader.ReadWord());
		}

		State.SetAutomaps(automaps);

		ResetStatusIcons();
		UpdatePartyMembers();
		SetMusic(!State.MusicBlock);
	}

	/// <summary>
	/// Loads the savegame and shows the saved location.
	/// </summary>
	/// <summary>
	/// A damaged savegame shows a message instead of ending the program.
	/// </summary>
	internal void TryLoadGameAndContinue(string? path)
	{
		try
		{
			LoadGameAndContinue(path);
		}
		catch (Exception ex) when (ex is InvalidDataException or IOException or AmberException or IndexOutOfRangeException or ArgumentException)
		{
			ResetGameState();
			ShowTextMessage(HasEnglishTexts ? "The savegame could not be loaded." : "Der Spielstand konnte nicht geladen werden.");
		}
	}

	internal void LoadGameAndContinue(string? path = null)
	{
		LoadGame(path);
		ScreenHandler.ClearAllScreens();
		Teleport(1 + State.PartyPosition.X, 1 + State.PartyPosition.Y, State.PartyDirection, State.MapIndex, true, true);
	}
}
