using Amber.Common;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Legacy.Translation;
using Amberstar.Translator;

namespace Amberstar.Tests;

public class TranslationTests
{
	static PoFile CreateTemplate(string folder) => TranslationTool.CreateTemplate([TestData.GetDataPath(folder)], []);

	static AssetProvider LoadTranslated(string folder, PoFile translation) =>
		TranslationTool.LoadGameData(TestData.GetDataPath(folder), translation);

	static PoEntry FindEntry(PoFile file, Func<PoEntry, bool> predicate) =>
		file.Entries.First(predicate);

	[Fact]
	public void TheTemplateOfTheRepositoryIsUpToDate()
	{
		// Translations/amberstar.pot has to be created again when the texts or the extraction change:
		// dotnet run --project Amberstar.Translator -- extract --output Translations/amberstar.pot
		var path = Path.Combine(Path.GetDirectoryName(TestData.GetDataPath("English/AmberfilesST"))!, "..", "Translations", "amberstar.pot");
		var repositoryTemplate = PoFile.Load(path);
		var template = TranslationTool.CreateTemplate(
			new[] { "English/AmberfilesAmiga", "English/AmberfilesST", "English/AmberfilesDOS" }.Select(TestData.GetDataPath), []);

		var expected = template.Entries.Select(e => (e.Context, e.Id)).ToHashSet();
		var actual = repositoryTemplate.Entries.Select(e => (e.Context, e.Id)).ToHashSet();

		Assert.True(expected.SetEquals(actual),
			$"Translations/amberstar.pot is outdated: {expected.Except(actual).Count()} new texts, {actual.Except(expected).Count()} removed texts.");
	}

	[Fact]
	public void PoFileKeepsTheTexts()
	{
		var file = new PoFile();
		file.SetHeader("Language", "fr");
		var entry = new PoEntry { Context = "maptext/001/000", Id = "LINE 1\nLINE \"2\"\\{P}END", Translation = "LIGNE 1\nLIGNE \"2\"\\{P}FIN" };
		entry.ExtractedComments.Add("A note.");
		entry.References.Add("maptext/001/000");
		entry.Flags.Add("fuzzy");
		file.Entries.Add(entry);
		file.Entries.Add(new PoEntry { Context = "word", Id = "SWORD", Translation = "ÉPÉE" });

		var read = PoFile.Read(file.Write());

		Assert.Equal("fr", read.GetHeader("Language"));
		Assert.Equal(2, read.Entries.Count);
		Assert.Equal(entry.Context, read.Entries[0].Context);
		Assert.Equal(entry.Id, read.Entries[0].Id);
		Assert.Equal(entry.Translation, read.Entries[0].Translation);
		Assert.Equal(["A note."], read.Entries[0].ExtractedComments);
		Assert.True(read.Entries[0].IsFuzzy);
		Assert.False(read.Entries[0].IsTranslated);
		Assert.Equal("ÉPÉE", read.Entries[1].Translation);
		Assert.True(read.Entries[1].IsTranslated);
	}

	[Theory]
	[InlineData("English/AmberfilesAmiga")]
	[InlineData("English/AmberfilesST")]
	[InlineData("English/AmberfilesDOS")]
	[InlineData("German/AmberfilesAmiga")]
	[InlineData("German/AmberfilesST")]
	public void TranslationWithTheSourceTextsKeepsAllTexts(string folder)
	{
		var original = TestData.CreateAssetProvider(folder);
		var template = CreateTemplate(folder);

		foreach (var entry in template.Entries)
			entry.Translation = entry.Id;

		var translated = LoadTranslated(folder, template);
		Assert.True(translated.HasTranslation);

		var originalTexts = TranslationExtractor.Extract(original).ToDictionary(unit => unit.Location + "|" + unit.Context, unit => unit.Source);
		var translatedTexts = TranslationExtractor.Extract(translated).ToDictionary(unit => unit.Location + "|" + unit.Context, unit => unit.Source);

		Assert.Equal(originalTexts.Count, translatedTexts.Count);

		foreach (var (key, source) in originalTexts)
			Assert.Equal(source, translatedTexts[key]);

		Assert.Empty(translated.InvalidTranslationCharacters);

		// the texts are wrapped like the original ones
		foreach (var key in original.GetAssetKeys(AssetType.MapText))
		{
			var originalText = original.TextLoader.LoadText(new AssetIdentifier(AssetType.MapText, key));
			var translatedText = translated.TextLoader.LoadText(new AssetIdentifier(AssetType.MapText, key));

			Assert.Equal(originalText.TextBlockCount, translatedText.TextBlockCount);

			for (int b = 0; b < originalText.TextBlockCount; b++)
				Assert.Equal(originalText.GetTextBlock(b).GetLines(30), translatedText.GetTextBlock(b).GetLines(30));
		}

		foreach (var message in Enum.GetValues<Message>())
		{
			Assert.Equal(original.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)message)).GetLines(30),
				translated.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)message)).GetLines(30));
		}

		var result = TranslationTool.Check(TestData.GetDataPath(folder), template);
		Assert.True(result.Errors.Count == 0, string.Join(Environment.NewLine, result.Errors));
		Assert.Equal(result.TextCount, result.TranslatedCount);
	}

	[Theory]
	[InlineData("English/AmberfilesAmiga")]
	[InlineData("English/AmberfilesST")]
	[InlineData("English/AmberfilesDOS")]
	public void TranslatedTextsAreUsed(string folder)
	{
		var original = TestData.CreateAssetProvider(folder);
		var template = CreateTemplate(folder);

		var mapText = FindEntry(template, e => e.Context.StartsWith("maptext/"));
		mapText.Translation = "IHR SEHT EINE TÜR (GESCHLOSSEN).\nSIE IST SEHR ALT, NICHT WAHR?{P}ENDE.";

		var message = FindEntry(template, e => e.Context == $"message/{(int)Message.NothingToSayAboutThat:000}");
		message.Translation = "DAZU WEIß ICH NICHTS!";

		var keyword = FindEntry(template, e => e.Context == "word" && e.References.Any(r => r.StartsWith("keyword/")) && !e.Id.Contains(' '));
		keyword.Translation = "ZAUBERWORT";

		var itemName = FindEntry(template, e => e.Context == "word" && e.References.Any(r => r.StartsWith("item/")) && e.References.Count == 1 && e.Id != keyword.Id);
		itemName.Translation = "SCHWERT DER MACHT";

		var characterName = FindEntry(template, e => e.Context == TranslationExtractor.CharacterNameContext);
		characterName.Translation = "JÜRGEN";

		var mapName = FindEntry(template, e => e.Context == TranslationExtractor.MapNameContext);
		mapName.Translation = "DIE STADT";

		var placeName = FindEntry(template, e => e.Context == TranslationExtractor.PlaceNameContext);
		placeName.Translation = "DAS GASTHAUS";

		var conversation = FindEntry(template, e => e.Context.StartsWith("conversation/"));
		conversation.Translation = "HALLO, IHR FREMDEN!";

		var gold = FindEntry(template, e => e.Context == "ui/Gold");
		gold.Translation = "GELD";

		var programText = FindEntry(template, e => e.Context == "program/GameSaved");
		programText.Translation = "Das Spiel wurde gespeichert.";

		var translated = LoadTranslated(folder, template);
		var textLoader = translated.TextLoader;

		// map text
		var mapTextParts = mapText.Context.Split('/');
		var lines = textLoader.LoadText(new AssetIdentifier(AssetType.MapText, int.Parse(mapTextParts[1]))).GetTextBlock(int.Parse(mapTextParts[2])).GetParagraphs(100);
		Assert.Equal(2, lines.Count);
		Assert.Equal(["IHR SEHT EINE TÜR (GESCHLOSSEN).", "SIE IST SEHR ALT, NICHT WAHR?"], lines[0].Select(line => GameCharset.ToText(line).TrimEnd()));
		Assert.Equal(["ENDE."], lines[1]);

		// message
		Assert.Equal("DAZU WEIß ICH NICHTS!", GameCharset.ToText(string.Join(" ",
			textLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)Message.NothingToSayAboutThat)).GetLines(100))));

		// the keyword keeps its index
		var originalIndex = original.TextLoader.FindWord(keyword.Id);
		Assert.NotNull(originalIndex);
		Assert.Equal(originalIndex, textLoader.FindWord("zauberwort"));

		// item name
		var item = translated.ItemLoader.LoadItem(uint.Parse(itemName.References[0].Split('/')[1]));
		Assert.Equal("SCHWERT DER MACHT", GameCharset.ToText(textLoader.FromTextFragmentIndex(((IItem)item).NameIndex).GetString()));

		// character name
		var person = translated.PersonLoader.LoadPerson(int.Parse(characterName.References[0].Split('/')[1]));
		Assert.Equal("JÜRGEN", GameCharset.ToText(person.Name));

		// map name
		Assert.Equal("DIE STADT", translated.MapLoader.LoadMap(int.Parse(mapName.References[0].Split('/')[1])).Name);

		// place name
		var placeReader = translated.GetAsset(new AssetIdentifier(AssetType.PlaceName, int.Parse(placeName.References[0].Split('/')[1])))!.GetReader();
		placeReader.Position = 0;
		Assert.Equal("DAS GASTHAUS", placeReader.ReadString(30).TrimEnd());

		// conversation
		var conversationParts = conversation.Context.Split('/');
		var speaker = translated.PersonLoader.LoadPerson(int.Parse(conversationParts[1]));
		Assert.Equal("HALLO, IHR FREMDEN!", GameCharset.ToText(string.Join(" ",
			speaker.ConversationData.Texts!.GetTextBlock(int.Parse(conversationParts[2])).GetLines(100))));
		Assert.Equal(original.PersonLoader.LoadPerson(int.Parse(conversationParts[1])).ConversationData.Texts!.TextBlockCount,
			speaker.ConversationData.Texts!.TextBlockCount);

		// user interface
		Assert.Equal("GELD", textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.Gold)).GetString());

		// texts of the program
		Assert.True(textLoader.HasTranslation);
		Assert.Equal("English", textLoader.TranslationSourceLanguage);
		Assert.Equal("Das Spiel wurde gespeichert.", textLoader.GetProgramText("GameSaved"));
		Assert.Null(textLoader.GetProgramText("SettingsTitle"));

		// untranslated texts stay as they are
		var otherMessage = (int)Message.NothingToSayAboutThat == 0 ? 1 : 0;
		Assert.Equal(original.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, otherMessage)).GetLines(30),
			textLoader.LoadText(new AssetIdentifier(AssetType.Message, otherMessage)).GetLines(30));

		var result = TranslationTool.Check(TestData.GetDataPath(folder), template);
		Assert.True(result.Errors.Count == 0, string.Join(Environment.NewLine, result.Errors));
	}

	[Fact]
	public void TheGameUsesAnInstalledTranslation()
	{
		const string folder = "English/AmberfilesST";
		var template = CreateTemplate(folder);
		FindEntry(template, e => e.Context == "program/MainMenuQuit").Translation = "AUFHÖREN";
		FindEntry(template, e => e.Context == "program/SpellHasNoEffect").Translation = "NICHTS PASSIERT MIT {0}.";
		FindEntry(template, e => e.Context == "ui/Male").Translation = "MANN";

		var outputFolder = Path.Combine(Path.GetTempPath(), "AmberstarTranslationTest-" + Guid.NewGuid().ToString("N"));

		try
		{
			TranslationTool.Install(TestData.GetDataPath(folder), template, outputFolder);
			Assert.True(File.Exists(Path.Combine(outputFolder, TranslationTool.TranslationFileName)));

			using var game = new Amberstar.Frontend.HeadlessGame(outputFolder, showMainMenu: true);
			game.Step(120);

			Assert.Equal("MainMenu", game.Game.ScriptActiveScreen);
			// the untranslated texts of the program are the English ones (the language of the translated data)
			Assert.True(game.Game.ScriptHasEnglishTexts);
			Assert.Equal("AUFH\u00d6REN", game.Game.GetProgramText(Amberstar.Game.ProgramText.MainMenuQuit));
			Assert.Equal("NICHTS PASSIERT MIT TOM.", game.Game.GetProgramText(Amberstar.Game.ProgramText.SpellHasNoEffect, "TOM"));
			Assert.Equal("LOAD GAME", game.Game.GetProgramText(Amberstar.Game.ProgramText.MainMenuLoadGame));
		}
		finally
		{
			Directory.Delete(outputFolder, true);
		}
	}

	[Fact]
	public void GlyphFileKeepsTheGlyphs()
	{
		var file = GlyphFile.Parse("# a comment\nА = A\n\nÉ\n..##.\n#####\n##...\n###..\n#####\n");

		Assert.Equal(2, file.Glyphs.Count);
		Assert.Equal('A', file.Find('А')!.Reference);
		Assert.Equal(["a comment"], file.Find('А')!.Comments);
		Assert.Equal(new byte[] { 0x30, 0xf8, 0xc0, 0xe0, 0xf8 }, file.Find('É')!.GetRowBytes());

		var read = GlyphFile.Parse(file.Write());
		Assert.Equal(file.Write(), read.Write());

		Assert.Throws<FormatException>(() => GlyphFile.Parse("É\n#####\n"));
		Assert.Throws<FormatException>(() => GlyphFile.Parse("А = A\nА = B\n"));
	}

	[Theory]
	[InlineData("English/AmberfilesAmiga")]
	[InlineData("English/AmberfilesST")]
	[InlineData("English/AmberfilesDOS")]
	public void CharactersOfTheGlyphFileAreShownAndStored(string folder)
	{
		var original = TestData.CreateAssetProvider(folder);
		var template = CreateTemplate(folder);

		var characterName = FindEntry(template, e => e.Context == TranslationExtractor.CharacterNameContext);
		characterName.Translation = "ŁUCJA É";
		var mapText = FindEntry(template, e => e.Context.StartsWith("maptext/"));
		mapText.Translation = "ŻÓŁW, ÉCOLE, ÇA.";
		var keyword = FindEntry(template, e => e.Context == "word" && e.References.Any(r => r.StartsWith("keyword/")) && !e.Id.Contains(' '));
		keyword.Translation = "ŁÓDŹ";
		FindEntry(template, e => e.Context == "ui/Gold").Translation = "ZŁOTO";
		FindEntry(template, e => e.Context == "program/MainMenuQuit").Translation = "ВЫХОД";
		var outroText = template.Entries.FirstOrDefault(e => e.Context == "outro/chancellor/00");

		if (outroText != null)
			outroText.Translation = "ПРИВЕТ, АМБЕРСТАР!";

		var missing = GlyphDrafts.FindMissingCharacters(original, template, null);
		Assert.All("ÇÉŁÓŹŻВЫХ", ch => Assert.Contains(ch, missing));
		// the lowercase letters use the glyphs of the uppercase letters
		Assert.DoesNotContain('ł', missing);

		var (glyphs, _, empty) = GlyphDrafts.AddDrafts(original, missing, null);
		Assert.Equal(0, empty);

		var translated = TranslationTool.LoadGameData(TestData.GetDataPath(folder), template, glyphs);
		var charset = translated.TranslationCharset!;
		var font = translated.FontLoader.LoadFont();
		Assert.Empty(charset.UnassignedCharacters);
		Assert.Empty(translated.InvalidTranslationCharacters);

		void AssertShown(string gameString) => Assert.Empty(GameCharset.GetCharactersWithoutGlyph(gameString, font, charset));

		// The name is stored in the character data (one byte per character).
		var person = translated.PersonLoader.LoadPerson(int.Parse(characterName.References[0].Split('/')[1]));
		Assert.Equal("ŁUCJA É", GameCharset.ToText(person.Name, charset));
		AssertShown(person.Name);

		var parts = mapText.Context.Split('/');
		var text = string.Join(" ", translated.TextLoader.LoadText(new AssetIdentifier(AssetType.MapText, int.Parse(parts[1])))
			.GetTextBlock(int.Parse(parts[2])).GetLines(100));
		Assert.Equal("ŻÓŁW, ÉCOLE, ÇA.", GameCharset.ToText(text, charset));
		AssertShown(text);

		// The É of the Atari ST character set gets its glyph at its place.
		var glyph = font.GetGlyph(FontCharacters.ToFontCharacter('É'), false)!;
		Assert.Equal(glyphs.Find('É')!.GetRowBytes(), Enumerable.Range(0, 5)
			.Select(y => (byte)Enumerable.Range(0, 8).Sum(x => glyph.GetData()[y * 8 + x] != 0 ? 0x80 >> x : 0)));

		// The keyword can be typed (also in lowercase).
		Assert.Equal(original.TextLoader.FindWord(keyword.Id), translated.TextLoader.FindWord("łódź"));

		var gold = translated.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.Gold)).GetString();
		Assert.Equal("ZŁOTO", GameCharset.ToText(gold, charset));
		AssertShown(gold);

		var quit = translated.TextLoader.GetProgramText("MainMenuQuit")!;
		Assert.Equal(5, quit.Length);
		AssertShown(quit);

		if (outroText != null)
		{
			var outro = translated.OutroData!;
			Assert.All(outro.ChancellorTexts[0].Where(ch => ch > ' '), ch => Assert.NotNull(outro.GetGlyph(ch)));
			// a reference shows the glyph of the other character
			Assert.Equal(outro.GetGlyph('A')!.GetData(), outro.GetGlyph('А')!.GetData());
		}

		var result = TranslationTool.Check(TestData.GetDataPath(folder), template, glyphs);
		Assert.True(result.Errors.Count == 0, string.Join(Environment.NewLine, result.Errors));
	}

	[Fact]
	public void TheGameShowsTheCharactersOfTheGlyphFile()
	{
		const string folder = "English/AmberfilesST";
		var template = CreateTemplate(folder);
		FindEntry(template, e => e.Context == "program/MainMenuQuit").Translation = "WYJŚCIE";
		var original = TestData.CreateAssetProvider(folder);
		var (glyphs, _, _) = GlyphDrafts.AddDrafts(original, GlyphDrafts.FindMissingCharacters(original, template, null), null);
		var outputFolder = Path.Combine(Path.GetTempPath(), "AmberstarGlyphTest-" + Guid.NewGuid().ToString("N"));

		try
		{
			TranslationTool.Install(TestData.GetDataPath(folder), template, outputFolder, glyphs);
			Assert.True(File.Exists(Path.Combine(outputFolder, GlyphFile.FileName)));

			using var game = new Amberstar.Frontend.HeadlessGame(outputFolder, showMainMenu: true);
			game.Step(120);

			var textLoader = game.Game.AssetProvider.TextLoader;
			Assert.True(game.Game.TextManager.HasGlyph(textLoader.ToGameText("Ś")[0]));
			Assert.True(game.Game.TextManager.HasGlyph(textLoader.ToGameText("ś")[0]));
			Assert.False(game.Game.TextManager.HasGlyph('Ś'));
			Assert.All(game.Game.GetProgramText(Amberstar.Game.ProgramText.MainMenuQuit), ch => Assert.True(game.Game.TextManager.HasGlyph(ch)));
		}
		finally
		{
			Directory.Delete(outputFolder, true);
		}
	}

	[Fact]
	public void AmbermoonTranslationsAreImportedAsFuzzy()
	{
		var root = Path.Combine(Path.GetTempPath(), "AmbermoonImportTest-" + Guid.NewGuid().ToString("N"));

		void Write(string language, string path, string text)
		{
			var file = Path.Combine(root, language, path);
			Directory.CreateDirectory(Path.GetDirectoryName(file)!);
			File.WriteAllText(file, text);
		}

		try
		{
			Write("English", "Text.amb/ClassNames/001.txt", "Warrior");
			Write("French", "Text.amb/ClassNames/001.txt", "Guerrier");
			Write("German", "Text.amb/ClassNames/001.txt", "Krieger");
			Write("English", "Text.amb/ConditionNames/011.txt", "Stoned");
			Write("French", "Text.amb/ConditionNames/011.txt", "Drogué");
			Write("German", "Text.amb/ConditionNames/011.txt", "Berauscht");
			// another meaning (a spell) is not used for a class name
			Write("English", "Objects.amb/010.txt", "Thief");
			Write("French", "Objects.amb/010.txt", "Voleur magique");
			Write("English", "Text.amb/UITexts/024.txt", "GOLD");
			Write("French", "Text.amb/UITexts/024.txt", "OR");
			Write("English", "Dict.amb/001.txt", "Warrior");
			Write("French", "Dict.amb/001.txt", "Mot");

			var translation = new PoFile();
			PoEntry Add(string context, string id, string reference, string? german = null, string? existing = null)
			{
				var entry = new PoEntry { Context = context, Id = id, Translation = existing ?? "" };
				entry.References.Add(reference);

				if (german != null)
					entry.ExtractedComments.Add($"German: {german}");

				translation.Entries.Add(entry);
				return entry;
			}

			var warrior = Add("word", "WARRIOR", "ClassName/001", "KRIEGER");
			var stoned = Add("word", "STONED", "ConditionName/003", "VERSTEINERT");
			var thief = Add("word", "THIEF", "ClassName/004");
			var gold = Add("ui/Gold", "{01}{08}GOLD  {01}{0F}", "ui/Gold");
			var translated = Add("word", "WARRIOR", "item/0001", existing: "GUERRIER!");

			var result = AmbermoonImport.Import(translation, Path.Combine(root, "English"), Path.Combine(root, "French"), Path.Combine(root, "German"));

			Assert.Equal(3, result.Imported);
			Assert.Equal("GUERRIER", warrior.Translation);
			Assert.True(warrior.IsFuzzy);
			Assert.DoesNotContain(warrior.TranslatorComments, c => c.Contains("check if it means the same"));
			Assert.Equal("DROGUÉ", stoned.Translation);
			Assert.Contains(stoned.TranslatorComments, c => c.Contains("BERAUSCHT") && c.Contains("VERSTEINERT"));
			Assert.Equal("", thief.Translation);
			Assert.Equal("{01}{08}OR    {01}{0F}", gold.Translation);
			Assert.Equal("GUERRIER!", translated.Translation);
			Assert.False(translated.IsFuzzy);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Theory]
	[InlineData("English/AmberfilesAmiga")]
	[InlineData("English/AmberfilesDOS")]
	public void NamesWithTheSameEnglishWordCanHaveDifferentTranslations(string folder)
	{
		var template = CreateTemplate(folder);
		var race = FindEntry(template, e => e.Context == "RaceName/000");
		var language = FindEntry(template, e => e.Context == "LanguageName/000");
		Assert.Equal("HUMAN", race.Id);
		Assert.Equal("HUMAN", language.Id);
		race.Translation = "MENSCH";
		language.Translation = "MENSCHLICH";

		var translated = LoadTranslated(folder, template);
		var textLoader = translated.TextLoader;

		Assert.Equal("MENSCH", textLoader.LoadText(new AssetIdentifier(AssetType.RaceName, 0)).GetString());
		Assert.Equal("MENSCHLICH", textLoader.LoadText(new AssetIdentifier(AssetType.LanguageName, 0)).GetString());
		// the dictionary word (e.g. used in the texts) stays
		var original = TestData.CreateAssetProvider(folder);
		var index = original.TextLoader.FindWord("HUMAN")!.Value;
		Assert.Equal("HUMAN", textLoader.FromTextFragmentIndex(index).GetString());
	}

	[Fact]
	public void TextsOfTheTextExportAreImported()
	{
		var root = Path.Combine(Path.GetTempPath(), "LegacyImportTest-" + Guid.NewGuid().ToString("N"));

		void Write(string path, string text)
		{
			var file = Path.Combine(root, path);
			Directory.CreateDirectory(Path.GetDirectoryName(file)!);
			File.WriteAllText(file, "\uFEFF" + text);
		}

		try
		{
			Write("ClassNames/001.txt", "VÁLEČNÍK\n");
			Write("MapTexts/004/000.txt", "ŘÁDEK 1: # ŘÁDEK 2 $ STRANA 2… ~ RUNY ~");
			Write("UITexts/009.txt", "Z:01234 J:0123");
			Write("UITexts/000.txt", "\u0001\u0008DOVEDNOSTI\u0001\u000f");
			Write("SpellNames/029.txt", "");
			Write("Unknown/001.txt", "X");

			var translation = new PoFile();
			PoEntry Add(string context, string id, string reference, string translated = "", bool fuzzy = false)
			{
				var entry = new PoEntry { Context = context, Id = id, Translation = translated };
				entry.References.Add(reference);

				if (fuzzy)
					entry.Flags.Add("fuzzy");

				translation.Entries.Add(entry);
				return entry;
			}

			var className = Add("ClassName/001", "WARRIOR", "ClassName/001", "GUERR.", fuzzy: true);
			var keyword = Add("word", "WARRIOR", "keyword/0066/0");
			var mapText = Add("maptext/004/000", "LINE 1:\nLINE 2{P}PAGE 2", "maptext/004/000");
			var goldFood = Add("ui/GoldFood", "G:01234 R:0123", "ui/GoldFood");
			var skills = Add("ui/Skills", "{01}{08}SKILLS{01}{0F}", "ui/Skills");

			var result = LegacyTextImport.Import(translation, root);

			Assert.Equal(4, result.Imported);
			Assert.Equal(1, result.Replaced);
			Assert.Equal(1, result.Suggested);
			Assert.Equal(["Unknown/001.txt"], result.Unknown);
			Assert.Equal("VÁLEČNÍK", className.Translation);
			Assert.False(className.IsFuzzy);
			Assert.Equal("VÁLEČNÍK", keyword.Translation);
			Assert.True(keyword.IsFuzzy);
			Assert.Equal("ŘÁDEK 1:\nŘÁDEK 2{P}STRANA 2... ~ RUNY ~", mapText.Translation);
			Assert.Equal("Z:01234 J:0123", goldFood.Translation);
			Assert.Equal("{01}{08}DOVEDNOSTI{01}{0F}", skills.Translation);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void CheckFindsErrors()
	{
		const string folder = "English/AmberfilesST";
		var template = CreateTemplate(folder);

		FindEntry(template, e => e.Context == TranslationExtractor.CharacterNameContext).Translation = "A NAME WHICH IS FAR TOO LONG";
		FindEntry(template, e => e.Context == "ui/Skills").Translation = "{01}COMPÉTENCES";
		FindEntry(template, e => e.Context == "program/CrumbledToDust").Translation = "IL EST POUSSIÈRE.";
		FindEntry(template, e => e.Context.StartsWith("maptext/")).Translation = "ЖЖЖ";

		var result = TranslationTool.Check(TestData.GetDataPath(folder), template);

		Assert.Contains(result.Errors, error => error.Contains("longer than 15 characters"));
		Assert.Contains(result.Errors, error => error.Contains("control codes"));
		Assert.Contains(result.Errors, error => error.Contains("placeholders"));
		Assert.Contains(result.Errors, error => error.Contains("'Ж'"));
	}

	[Fact]
	public void MergeKeepsTheTranslations()
	{
		var translation = new PoFile();
		translation.SetHeader("Language", "fr");
		translation.Entries.Add(new PoEntry { Context = "word", Id = "SWORD", Translation = "ÉPÉE" });
		translation.Entries.Add(new PoEntry { Context = "maptext/001/000", Id = "OLD TEXT", Translation = "VIEUX TEXTE" });
		translation.Entries.Add(new PoEntry { Context = "maptext/002/000", Id = "REMOVED", Translation = "SUPPRIMÉ" });

		var template = new PoFile();
		template.Entries.Add(new PoEntry { Context = "word", Id = "SWORD" });
		template.Entries.Add(new PoEntry { Context = "maptext/001/000", Id = "NEW TEXT" });
		template.Entries.Add(new PoEntry { Context = "word", Id = "SHIELD" });

		var (result, kept, fuzzy, obsolete) = TranslationTool.Merge(translation, template);

		Assert.Equal(1, kept);
		Assert.Equal(1, fuzzy);
		Assert.Equal(1, obsolete);
		Assert.Equal("fr", result.GetHeader("Language"));
		Assert.Equal("ÉPÉE", result.Entries[0].Translation);
		Assert.Equal("VIEUX TEXTE", result.Entries[1].Translation);
		Assert.True(result.Entries[1].IsFuzzy);
		Assert.Equal("", result.Entries[2].Translation);
	}
}
