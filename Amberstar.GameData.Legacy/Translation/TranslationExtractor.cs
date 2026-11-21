using Amber.Common;
using Amberstar.GameData.Events;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// A text of the game data at one place.
/// </summary>
/// <param name="Context">The key of the translation (msgctxt), e.g. "maptext/012/003" or "word".</param>
/// <param name="Source">The text as normal text (see <see cref="TextFormat"/> and <see cref="GameCharset"/>).</param>
/// <param name="Location">Where the text is used, e.g. "item/0042". It is unique for each text of the game data.</param>
/// <param name="Note">A hint for the translators.</param>
/// <param name="MaxLength">The maximum length of the translation or 0.</param>
public sealed record TranslationUnit(string Context, string Source, string Location, string Note, int MaxLength = 0);

/// <summary>
/// Extracts all texts of the game data which can be translated. The keys (contexts) are the same for all
/// versions (Amiga, Atari ST, DOS and the languages), so a translation can be used for all versions of the
/// language it was made from.
///
/// The contexts are:
///  - word: a single word of the dictionary which is used on its own (names of items, conversation keywords and
///    the answers of the riddle mouths). It replaces the word everywhere.
///  - ClassName/NNN, RaceName/NNN, SpellName/NNN, ...: the names of the program data (classes, races, attributes,
///    skills, languages, conditions, item types, spell schools and spells). Each gets its own word.
///  - maptext/MMM/BBB, itemtext/III/BBB, puzzletext/III/BBB, message/NNN, conversation/CCCC/BBB: texts which
///    are made of words of the dictionary.
///  - ui/NAME: the texts of the user interface (with control codes).
///  - name/character, name/map, name/place: names of characters, maps and places (with a maximum length).
///  - outro/chancellor/NN, outro/balcony/NN: the texts of the outro.
/// </summary>
public static class TranslationExtractor
{
	public const string WordContext = "word";
	public const string CharacterNameContext = "name/character";
	public const string MapNameContext = "name/map";
	public const string PlaceNameContext = "name/place";
	public const int MaxCharacterNameLength = 15;
	public const int MaxMapNameLength = 30;
	public const int MaxPlaceNameLength = 30;

	internal static readonly AssetType[] SingleWordAssetTypes =
	[
		AssetType.ClassName, AssetType.RaceName, AssetType.AttributeName, AssetType.SkillName, AssetType.CharInfoText,
		AssetType.LanguageName, AssetType.ConditionName, AssetType.ItemTypeName, AssetType.SpellSchoolName, AssetType.SpellName,
	];

	/// <summary>
	/// True for the contexts of the names of the program data (e.g. "ClassName/001").
	/// </summary>
	public static bool IsNameOfProgramData(string context) =>
		SingleWordAssetTypes.Any(type => context.StartsWith(type.ToString() + "/", StringComparison.Ordinal));

	/// <summary>
	/// True for texts made of words (map texts, messages, conversations, ...) and UI and outro texts. Their
	/// translation is also used for the same source text at another place (e.g. of another version).
	/// </summary>
	public static bool IsText(string context) => context != WordContext && !context.StartsWith("name/") &&
		!context.StartsWith("program/") && !IsNameOfProgramData(context);

	public static List<TranslationUnit> Extract(AssetProvider assetProvider)
	{
		var units = new List<TranslationUnit>();
		var textLoader = assetProvider.TextLoader;
		var dictionary = ((Text)textLoader.FromTextFragmentIndex(0)).Dictionary;
		static string ToText(string gameString) => GameCharset.ToText(gameString);

		void AddWord(int index, string location, string note)
		{
			if (index <= 0 || index >= dictionary.Count || dictionary[index].Trim().Length == 0)
				return;

			units.Add(new(WordContext, ToText(dictionary[index]), location, note));
		}

		void AddText(string context, IText text, string note)
		{
			var words = ((Text)text).WordIndices;
			var source = TextFormat.Format(words, dictionary);

			if (source.Trim().Length != 0)
				units.Add(new(context, source, context, note));
		}

		void AddTextBlocks(string contextPrefix, IText text, string note)
		{
			for (int b = 0; b < text.TextBlockCount; b++)
				AddText($"{contextPrefix}/{b:000}", text.GetTextBlock(b), note);
		}

		// single words of the program data
		foreach (var type in SingleWordAssetTypes)
		{
			foreach (var key in assetProvider.GetAssetKeys(type).Order())
			{
				var text = (Text)textLoader.LoadText(new AssetIdentifier(type, key));

				// Each name has its own key: the same English word can need different translations (e.g. the race
				// HUMAN and the language HUMAN).
				if (text.WordIndices.Count != 0 && text.WordIndices[0] > 0 && text.WordIndices[0] < dictionary.Count &&
					dictionary[text.WordIndices[0]].Trim().Length != 0)
				{
					var location = $"{type}/{key:000}";
					units.Add(new(location, ToText(dictionary[text.WordIndices[0]]), location, WordNote(type)));
				}
			}
		}

		foreach (var item in assetProvider.ItemLoader.LoadAllItems().OrderBy(item => item.Index))
			AddWord(((IItem)item).NameIndex, $"item/{item.Index:0000}", "Item name (keep it about as long as the English names, the longest has 20 characters).");

		// characters (persons, party members and monsters)
		var personKeys = assetProvider.GetAssetKeys(AssetType.Person).Order().ToList();

		foreach (var key in personKeys)
		{
			var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Person, key))!;
			var reader = asset.GetReader();
			reader.Position = 0x02;
			bool monster = (CharacterType)reader.ReadByte() == CharacterType.Monster;
			reader.Position = 0xf0;
			var name = reader.ReadString(16).TrimEnd(' ', '\0');

			if (name.Length != 0)
			{
				units.Add(new(CharacterNameContext, ToText(name), $"character/{key:0000}",
					monster ? "Name of a monster." : "Name of a person.", MaxCharacterNameLength));
			}

			if (monster)
				continue;

			var person = assetProvider.PersonLoader.LoadPerson(key);
			var conversation = person.ConversationData;
			int keywordIndex = 0;

			foreach (var (trigger, interaction) in conversation.PrimaryInteractions.Concat(conversation.SecondaryInteractions))
			{
				if (trigger.Type == InteractionTriggerType.Say)
				{
					AddWord(trigger.Argument, $"keyword/{key:0000}/{keywordIndex++}",
						"Conversation keyword: the player types it in a conversation. It must be a single word and must be unique.");
				}

				foreach (var reaction in interaction.Reactions)
				{
					if (reaction is ITeachWordReaction teachWord)
					{
						AddWord(teachWord.WordIndex, $"keyword/{key:0000}/{keywordIndex++}",
							"Conversation keyword: the player types it in a conversation. It must be a single word and must be unique.");
					}
				}
			}

			if (conversation.Texts != null)
			{
				AddTextBlocks($"conversation/{key:0000}", conversation.Texts,
					$"Conversation text of {ToText(name)}.");
			}
		}

		// maps
		foreach (var (index, map) in assetProvider.MapLoader.LoadAllMaps().OrderBy(map => map.Key))
		{
			if (map.Name.Length != 0)
				units.Add(new(MapNameContext, ToText(map.Name), $"map/{index:000}", "Name of a map.", MaxMapNameLength));

			{
				for (int e = 0; e < map.Events.Count; e++)
				{
					var @event = map.Events[e];

					if (@event is IRiddleMouthEvent riddleMouth)
					{
						AddWord(riddleMouth.WordIndex, $"event/{index:000}/{e + 1:000}",
							"Answer of a riddle mouth: the player types it. It must be a single word and must be unique.");
					}
					else if (@event is IShowPictureTextEvent pictureText && pictureText.SetWordBit != 0)
					{
						AddWord(pictureText.SetWordBit, $"event/{index:000}/{e + 1:000}", "A word which the party learns from a text.");
					}
				}
			}
		}

		foreach (var key in assetProvider.GetAssetKeys(AssetType.MapText).Order())
			AddTextBlocks($"maptext/{key:000}", textLoader.LoadText(new AssetIdentifier(AssetType.MapText, key)), "Text of a map.");

		foreach (var key in assetProvider.GetAssetKeys(AssetType.ItemText).Order())
			AddTextBlocks($"itemtext/{key:000}", textLoader.LoadText(new AssetIdentifier(AssetType.ItemText, key)), "Text of an item (e.g. a letter or book).");

		foreach (var key in assetProvider.GetAssetKeys(AssetType.PuzzleText).Order())
			AddTextBlocks($"puzzletext/{key:000}", textLoader.LoadText(new AssetIdentifier(AssetType.PuzzleText, key)), "Text of the altar puzzle.");

		int messageIndex = 0;

		foreach (var key in assetProvider.GetAssetKeys(AssetType.Message).Order())
		{
			// LoadText of a message gives a single block, so the containers are read directly.
			var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Message, key))!;
			var reader = asset.GetReader();
			reader.Position = 0;
			var blocks = Text.Read(reader, dictionary);
			reader.Position = 0;

			for (int b = 0; b < blocks.TextBlockCount; b++)
			{
				var message = messageIndex + b;
				AddText($"message/{message:000}", blocks.GetTextBlock(b),
					Enum.IsDefined((Message)message) ? $"Message {(Message)message}." : "Message.");
			}

			messageIndex += blocks.TextBlockCount;
		}

		foreach (var uiText in Enum.GetValues<UIText>())
		{
			var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.UIText, (int)uiText));

			if (asset == null)
				continue;

			var reader = asset.GetReader();
			reader.Position = 0;
			var text = reader.ReadString(reader.Size);
			reader.Position = 0;

			units.Add(new($"ui/{uiText}", ToText(text), $"ui/{uiText}",
				"Text of the user interface. Keep the codes like {01} (colors) and use the same length: the spaces are used for the layout and the values."));
		}

		foreach (var key in assetProvider.GetAssetKeys(AssetType.PlaceName).Order())
		{
			var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.PlaceName, key))!;
			var reader = asset.GetReader();
			reader.Position = 0;
			var name = reader.ReadString(Math.Min(MaxPlaceNameLength, reader.Size)).TrimEnd(' ', '\0');
			reader.Position = 0;

			if (name.Length != 0)
				units.Add(new(PlaceNameContext, ToText(name), $"place/{key:000}", "Name of a place (shop, inn, guild, ...).", MaxPlaceNameLength));
		}

		if (assetProvider.OutroData is IOutroData outro)
		{
			void AddOutroTexts(string name, IReadOnlyList<string> texts)
			{
				for (int i = 0; i < texts.Count; i++)
				{
					int maxLineLength = texts[i].Split('\n').Max(line => line.Length);
					units.Add(new($"outro/{name}/{i:00}", texts[i], $"outro/{name}/{i:00}",
						$"Text of the outro. The lines must not be longer than {maxLineLength} characters (the length of the longest English line)."));
				}
			}

			AddOutroTexts("chancellor", outro.ChancellorTexts);
			AddOutroTexts("balcony", outro.BalconyTexts);
		}

		return units;
	}

	static string WordNote(AssetType type) => type switch
	{
		AssetType.ClassName => "Class name.",
		AssetType.RaceName => "Race name.",
		AssetType.AttributeName => "Attribute name (the first 3 characters are used as abbreviation).",
		AssetType.SkillName => "Skill name (the first 3 characters are used as abbreviation).",
		AssetType.CharInfoText => "Text of the character info.",
		AssetType.LanguageName => "Language name.",
		AssetType.ConditionName => "Condition name.",
		AssetType.ItemTypeName => "Item type name.",
		AssetType.SpellSchoolName => "Spell school name.",
		AssetType.SpellName => "Spell name.",
		_ => type.ToString()
	};
}
