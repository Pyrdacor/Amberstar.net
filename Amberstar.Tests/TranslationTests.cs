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
