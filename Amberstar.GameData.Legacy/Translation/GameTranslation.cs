using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.FileSystem;
using Amber.IO.Common.Serialization;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// A translation of the texts of the game data. It is the PO file <see cref="FileName"/> in the folder of the
/// game data which it was made from (see <see cref="TranslationExtractor"/> for the keys). The asset provider
/// replaces the texts with the translated ones while they are loaded. Texts without a translation stay as
/// they are.
///
/// The translated texts are split into words. Words which are not part of the dictionary are added to it.
/// The single words (names and keywords) replace the words of the dictionary, so their indices (used by
/// the items, conversations and savegames) stay the same.
/// </summary>
internal sealed class GameTranslation
{
	public const string FileName = "TRANSLAT.PO";
	const string ProgramTextPrefix = "program/";

	readonly Dictionary<(string Context, string Source), string> translations = [];
	// For texts whose source text differs from the one of the key (e.g. other versions), the
	// translation of the same source text is used.
	readonly Dictionary<string, string> textTranslationsBySource = [];
	readonly Dictionary<string, string> programTexts = [];
	readonly Dictionary<string, word> keywordIndices = new(StringComparer.OrdinalIgnoreCase);
	List<string> originalDictionary = [];
	List<string> dictionary = [];
	readonly Dictionary<string, word> wordIndices = [];

	public GameTranslation(PoFile file)
	{
		File = file;
		SourceLanguage = file.GetHeader("X-Source-Language");

		foreach (var entry in file.Entries)
		{
			if (!entry.IsTranslated)
				continue;

			translations.TryAdd((entry.Context, entry.Id), entry.Translation);

			if (entry.Context.StartsWith(ProgramTextPrefix))
				programTexts.TryAdd(entry.Context[ProgramTextPrefix.Length..], entry.Translation);

			if (IsTextContext(entry.Context))
				textTranslationsBySource.TryAdd(entry.Id, entry.Translation);
		}
	}

	public PoFile File { get; }

	public string? SourceLanguage { get; }

	/// <summary>
	/// Characters of the translations which are not part of the character set of the game.
	/// </summary>
	public HashSet<char> InvalidCharacters { get; } = [];

	static bool IsTextContext(string context) => context != TranslationExtractor.WordContext && !context.StartsWith("name/") && !context.StartsWith(ProgramTextPrefix);

	public static GameTranslation? Load(IReadOnlyFileSystem fileSystem)
	{
		var file = fileSystem.GetFile(FileName);

		if (file == null)
			return null;

		using var reader = file.Stream.GetReader();
		reader.Position = 0;

		return new GameTranslation(PoFile.Read(System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.Size))));
	}

	public string? GetTranslation(string context, string source)
	{
		if (translations.TryGetValue((context, source), out var translation))
			return translation;

		return IsTextContext(context) ? textTranslationsBySource.GetValueOrDefault(source) : null;
	}

	/// <summary>
	/// The translation of a text of the program which is not part of the game data (e.g. the main menu).
	/// </summary>
	public string? GetProgramText(string key) => programTexts.GetValueOrDefault(key);

	/// <summary>
	/// The index of a translated keyword (the words which the player can type).
	/// </summary>
	public word? FindKeyword(string word) => keywordIndices.TryGetValue(word, out var index) ? index : null;

	/// <summary>
	/// Translates the words of the dictionary and the texts of the program data.
	/// </summary>
	public void Apply(ProgramData data)
	{
		dictionary = data.TextFragments;
		originalDictionary = [.. dictionary];

		for (int i = 1; i < dictionary.Count; i++)
		{
			var translation = GetTranslation(TranslationExtractor.WordContext, GameCharset.ToText(originalDictionary[i]));

			if (translation != null)
			{
				dictionary[i] = GameCharset.ToGameString(translation, InvalidCharacters);
				keywordIndices.TryAdd(dictionary[i].Trim(), (word)i);
			}
		}

		for (int i = 0; i < dictionary.Count; i++)
			wordIndices.TryAdd(dictionary[i], (word)i);

		foreach (var key in data.PlaceNames.Keys.ToList())
		{
			var bytes = ReadAll(data.PlaceNames[key]);
			var name = DataReader.Encoding.GetString(bytes, 0, Math.Min(TranslationExtractor.MaxPlaceNameLength, bytes.Length)).TrimEnd(' ', '\0');

			if (TranslateName(TranslationExtractor.PlaceNameContext, name, TranslationExtractor.MaxPlaceNameLength) is string translated)
			{
				var newName = GameCharset.ToBytes(translated.PadRight(TranslationExtractor.MaxPlaceNameLength));

				for (int c = 0; c < newName.Length && c < bytes.Length; c++)
					bytes[c] = newName[c];
			}

			data.PlaceNames[key] = new DataReader(bytes);
		}

		foreach (var key in data.UITexts.Keys.ToList())
		{
			var bytes = ReadAll(data.UITexts[key]);
			var translation = GetTranslation($"ui/{(UIText)key}", GameCharset.ToText(DataReader.Encoding.GetString(bytes)));

			if (translation != null)
				bytes = GameCharset.ToBytes(GameCharset.ToGameString(translation, InvalidCharacters));

			data.UITexts[key] = new DataReader(bytes);
		}

		int messageIndex = 0;

		for (int i = 0; i < data.MessageData.Length; i++)
		{
			var container = Text.Read(new DataReader(ReadAll(data.MessageData[i])), originalDictionary);
			int firstIndex = messageIndex;
			data.MessageData[i] = new DataReader(TranslateTextBlocks(container, block => $"message/{firstIndex + block:000}"));
			messageIndex += container.TextBlockCount;
		}
	}

	static byte[] ReadAll(IDataReader reader)
	{
		reader.Position = 0;
		var bytes = reader.ReadBytes(reader.Size);
		reader.Position = 0;

		return bytes;
	}

	string? TranslateName(string context, string name, int maxLength)
	{
		var translation = GetTranslation(context, GameCharset.ToText(name));

		if (translation == null)
			return null;

		var gameString = GameCharset.ToGameString(translation, InvalidCharacters);

		return gameString.Length > maxLength ? gameString[..maxLength] : gameString;
	}

	word? FindExistingWord(string word) => wordIndices.TryGetValue(word, out var index) ? index : null;

	word FindOrAddWord(string word)
	{
		if (wordIndices.TryGetValue(word, out var index))
			return index;

		if (dictionary.Count > ushort.MaxValue)
			throw new AmberException(ExceptionScope.Data, "Too many words in the dictionary of the translation.");

		index = (word)dictionary.Count;
		dictionary.Add(word);
		wordIndices.Add(word, index);

		return index;
	}

	/// <summary>
	/// Gives the words of a translated text.
	/// </summary>
	public List<word> ParseText(string text) => TextFormat.Parse(text, dictionary, FindExistingWord, FindOrAddWord, InvalidCharacters);

	/// <summary>
	/// The source text of the original words.
	/// </summary>
	string FormatOriginal(IEnumerable<word> words) => TextFormat.Format(words, originalDictionary);

	byte[] TranslateTextBlocks(Text container, Func<int, string> getContext)
	{
		var blocks = new List<IReadOnlyList<word>>(container.TextBlockCount);

		for (int b = 0; b < container.TextBlockCount; b++)
		{
			var words = ((Text)container.GetTextBlock(b)).WordIndices;
			var translation = GetTranslation(getContext(b), FormatOriginal(words));
			blocks.Add(translation == null ? words : ParseText(translation));
		}

		return WriteTextBlocks(blocks);
	}

	/// <summary>
	/// Writes text blocks in the format which <see cref="Text.Read"/> reads.
	/// </summary>
	static byte[] WriteTextBlocks(List<IReadOnlyList<word>> blocks)
	{
		if (blocks.Count > byte.MaxValue)
			throw new AmberException(ExceptionScope.Data, "Too many text blocks.");

		var writer = new DataWriter();
		writer.Write((byte)blocks.Count);
		writer.Write((byte)0);

		if (blocks.Count == 0)
			return writer.ToArray();

		int offset = 0;
		writer.Write((word)offset);

		foreach (var block in blocks)
		{
			offset += block.Count;
			writer.Write((word)offset);
		}

		foreach (var block in blocks)
		{
			foreach (var index in block)
				writer.Write(index);
		}

		return writer.ToArray();
	}

	/// <summary>
	/// Translates an asset of the data files (texts, maps and characters).
	/// </summary>
	public IAsset Translate(IAsset asset)
	{
		var identifier = asset.Identifier;

		string? textContextPrefix = identifier.Type switch
		{
			AssetType.MapText => $"maptext/{identifier.Index:000}",
			AssetType.ItemText => $"itemtext/{identifier.Index:000}",
			AssetType.PuzzleText => $"puzzletext/{identifier.Index:000}",
			_ => null
		};

		if (textContextPrefix != null)
		{
			var container = Text.Read(new DataReader(ReadAll(asset.GetReader())), originalDictionary);

			return new Asset(identifier, new DataReader(TranslateTextBlocks(container, block => $"{textContextPrefix}/{block:000}")));
		}

		return identifier.Type switch
		{
			AssetType.Map => new Asset(identifier, new DataReader(TranslateMap(ReadAll(asset.GetReader())))),
			AssetType.Person => new Asset(identifier, new DataReader(TranslateCharacter(identifier.Index, ReadAll(asset.GetReader())))),
			_ => asset
		};
	}

	// the name of a map follows the header values (magic, fill, tileset, type, flags, music, width and height)
	const int MapNameOffset = 9;
	const int MapNameSize = 31;

	byte[] TranslateMap(byte[] data)
	{
		if (data.Length < MapNameOffset + MapNameSize)
			return data;

		var name = DataReader.Encoding.GetString(data, MapNameOffset, MapNameSize).Split('\0')[0].TrimEnd();

		if (TranslateName(TranslationExtractor.MapNameContext, name, TranslationExtractor.MaxMapNameLength) is string translated)
		{
			var bytes = GameCharset.ToBytes(translated);

			for (int c = 0; c < MapNameSize; c++)
				data[MapNameOffset + c] = c < bytes.Length ? bytes[c] : (byte)0;
		}

		return data;
	}

	const int CharacterNameOffset = 0xf0;
	const int CharacterNameSize = 16;
	// see ConversationData.Load
	const int PortraitOffset = 0x6aa;

	byte[] TranslateCharacter(int index, byte[] data)
	{
		if (data.Length < CharacterNameOffset + CharacterNameSize)
			return data;

		var name = DataReader.Encoding.GetString(data, CharacterNameOffset, CharacterNameSize).TrimEnd(' ', '\0');

		if (TranslateName(TranslationExtractor.CharacterNameContext, name, TranslationExtractor.MaxCharacterNameLength) is string translated)
		{
			var bytes = GameCharset.ToBytes(translated);

			for (int c = 0; c < CharacterNameSize; c++)
				data[CharacterNameOffset + c] = c < bytes.Length ? bytes[c] : (byte)0;
		}

		if (data[2] == (byte)CharacterType.Monster)
			return data;

		// The texts of a conversation follow the portrait.
		var reader = new DataReader(data);
		reader.Position = PortraitOffset;

		if (reader.Position > reader.Size - 2)
			return data;

		if (reader.PeekWord() == 0)
			reader.Position += 2;
		else
			GraphicLoader.LoadGraphicWithHeader(reader);

		if (reader.Position > reader.Size - 2 || reader.PeekWord() == 0)
			return data;

		int textStart = reader.Position;
		var texts = Text.Read(reader, originalDictionary);
		int textEnd = reader.Position;
		var translatedTexts = TranslateTextBlocks(texts, block => $"conversation/{index:0000}/{block:000}");

		return [.. data.AsSpan(0, textStart), .. translatedTexts, .. data.AsSpan(textEnd)];
	}

	/// <summary>
	/// Translates the texts of the outro.
	/// </summary>
	public void Apply(OutroData outro)
	{
		List<string> Translate(string name, IReadOnlyList<string> texts) =>
			[.. texts.Select((text, index) => GetTranslation($"outro/{name}/{index:00}", text)?.Replace("\r\n", "\n") ?? text)];

		outro.ChancellorTexts = Translate("chancellor", outro.ChancellorTexts);
		outro.BalconyTexts = Translate("balcony", outro.BalconyTexts);
	}
}
