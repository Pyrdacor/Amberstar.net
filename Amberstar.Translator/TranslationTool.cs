using System.Text.RegularExpressions;
using Amber.IO.Common.FileSystem;
using Amber.IO.FileSystem;
using Amberstar.Game;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Legacy.Translation;

namespace Amberstar.Translator;

/// <summary>
/// Creates the translation templates (PO files) from the game data and checks and installs translations.
/// </summary>
public static partial class TranslationTool
{
	public const string TranslationFileName = "TRANSLAT.PO";
	const string ProgramContextPrefix = "program/";
	// Longer words can't be wrapped in the text windows.
	const int MaxWordLength = 25;

	public static AssetProvider LoadGameData(string folder) => new(FileSystem.FromOperatingSystemPath(folder).AsReadOnly());

	/// <summary>
	/// The game data of the folder with the given translation (without changing the folder).
	/// </summary>
	public static AssetProvider LoadGameData(string folder, PoFile translation)
	{
		var fileSystem = FileSystem.CreateVirtual();

		foreach (var file in Directory.GetFiles(folder))
		{
			if (!Path.GetFileName(file).Equals(TranslationFileName, StringComparison.OrdinalIgnoreCase))
				fileSystem.CreateFile(Path.GetFileName(file), File.ReadAllBytes(file));
		}

		fileSystem.CreateFile(TranslationFileName, System.Text.Encoding.UTF8.GetBytes(translation.Write()));

		return new AssetProvider(fileSystem.AsReadOnly());
	}

	/// <summary>
	/// "English" or "German" (the language of the user interface texts).
	/// </summary>
	public static string DetectLanguage(AssetProvider assetProvider) =>
		assetProvider.TextLoader.LoadText(new(Amber.Common.AssetType.UIText, (int)UIText.Male)).GetString().Contains("MALE") ? "English" : "German";

	/// <summary>
	/// Creates the template for the translators from the texts of the given game data folders (e.g. the English
	/// Amiga, Atari ST and DOS data). The texts of the reference folders (e.g. the German data) are added as
	/// comments for the translators.
	/// </summary>
	public static PoFile CreateTemplate(IEnumerable<string> folders, IEnumerable<string> referenceFolders, Action<string>? log = null)
	{
		var entries = new Dictionary<(string Context, string Source), PoEntry>();
		var references = new Dictionary<string, (string Language, string Text)>();
		string? sourceLanguage = null;

		foreach (var folder in referenceFolders)
		{
			var assetProvider = LoadGameData(folder);
			var language = DetectLanguage(assetProvider);

			foreach (var unit in TranslationExtractor.Extract(assetProvider))
				references.TryAdd(unit.Location, (language, unit.Source));

			foreach (var (key, text) in ProgramTexts.Texts)
				references.TryAdd(ProgramContextPrefix + key, (language, language == "German" ? text.German : text.English));

			log?.Invoke($"Read the reference texts of {folder} ({language}).");
		}

		foreach (var folder in folders)
		{
			var assetProvider = LoadGameData(folder);

			if (assetProvider.HasTranslation)
				throw new InvalidOperationException($"The game data of {folder} is already translated.");

			var language = DetectLanguage(assetProvider);

			if (sourceLanguage != null && sourceLanguage != language)
				throw new InvalidOperationException("All game data folders must have the same language.");

			sourceLanguage = language;
			var units = TranslationExtractor.Extract(assetProvider);

			foreach (var unit in units)
			{
				if (!entries.TryGetValue((unit.Context, unit.Source), out var entry))
				{
					entry = new PoEntry { Context = unit.Context, Id = unit.Source };
					entries.Add((unit.Context, unit.Source), entry);

					if (unit.MaxLength != 0)
						entry.ExtractedComments.Add($"Max. {unit.MaxLength} characters.");

					if (references.TryGetValue(unit.Location, out var reference) && reference.Language != language)
						AddReferenceComment(entry, reference.Language, reference.Text);
				}

				if (!entry.ExtractedComments.Contains(unit.Note))
					entry.ExtractedComments.Insert(0, unit.Note);

				if (!entry.References.Contains(unit.Location))
					entry.References.Add(unit.Location);
			}

			log?.Invoke($"Extracted {units.Count} texts of {folder} ({language}).");
		}

		foreach (var (key, text) in ProgramTexts.Texts)
		{
			var context = ProgramContextPrefix + key;
			var entry = new PoEntry { Context = context, Id = sourceLanguage == "German" ? text.German : text.English };
			entry.ExtractedComments.Add($"Text of Amberstar.net (not part of the original game). {text.Note}");
			entry.References.Add(context);

			if (references.TryGetValue(context, out var reference) && reference.Language != sourceLanguage)
				AddReferenceComment(entry, reference.Language, reference.Text);

			entries.TryAdd((context, entry.Id), entry);
		}

		var file = new PoFile();
		file.SetHeader("Project-Id-Version", "Amberstar.net");
		file.SetHeader("Language", "");
		file.SetHeader("MIME-Version", "1.0");
		file.SetHeader("Content-Type", "text/plain; charset=UTF-8");
		file.SetHeader("Content-Transfer-Encoding", "8bit");
		file.SetHeader("X-Source-Language", sourceLanguage ?? "English");
		file.Entries.AddRange(entries.Values);

		return file;
	}

	static void AddReferenceComment(PoEntry entry, string language, string text)
	{
		var lines = text.Split('\n');
		entry.ExtractedComments.Add($"{language}: {lines[0]}");

		foreach (var line in lines.Skip(1))
			entry.ExtractedComments.Add($"  {line}");
	}

	/// <summary>
	/// Takes the translations of an existing translation for a new template (e.g. after the extraction was
	/// improved). Translations of changed source texts are marked as fuzzy.
	/// </summary>
	public static (PoFile Result, int Kept, int Fuzzy, int Obsolete) Merge(PoFile translation, PoFile template)
	{
		var byKey = translation.Entries.GroupBy(e => (e.Context, e.Id)).ToDictionary(g => g.Key, g => g.First());
		var byContext = translation.Entries.Where(e => e.Translation.Length != 0).GroupBy(e => e.Context).ToDictionary(g => g.Key, g => g.First());
		var bySource = translation.Entries.Where(e => e.IsTranslated).GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First());
		var used = new HashSet<PoEntry>();
		var result = new PoFile();
		int kept = 0, fuzzy = 0;

		result.Header.AddRange(template.Header);

		foreach (var (key, value) in translation.Header)
		{
			if (key is not ("X-Source-Language" or "Project-Id-Version" or "Content-Type" or "MIME-Version" or "Content-Transfer-Encoding"))
				result.SetHeader(key, value);
		}

		foreach (var templateEntry in template.Entries)
		{
			var entry = new PoEntry { Context = templateEntry.Context, Id = templateEntry.Id };
			entry.ExtractedComments.AddRange(templateEntry.ExtractedComments);
			entry.References.AddRange(templateEntry.References);

			if (byKey.TryGetValue((entry.Context, entry.Id), out var old))
			{
				entry.Translation = old.Translation;
				entry.TranslatorComments.AddRange(old.TranslatorComments);
				entry.Flags.AddRange(old.Flags);
				used.Add(old);

				if (old.Translation.Length != 0)
					kept++;
			}
			else if (byContext.TryGetValue(entry.Context, out old) && entry.Context != TranslationExtractor.WordContext && !entry.Context.StartsWith("name/"))
			{
				entry.Translation = old.Translation;
				entry.TranslatorComments.AddRange(old.TranslatorComments);
				entry.Flags.Add("fuzzy");
				used.Add(old);
				fuzzy++;
			}
			else if (bySource.TryGetValue(entry.Id, out old))
			{
				entry.Translation = old.Translation;
				used.Add(old);
				kept++;
			}

			result.Entries.Add(entry);
		}

		int obsolete = translation.Entries.Count(e => e.Translation.Length != 0 && !used.Contains(e));

		return (result, kept, fuzzy, obsolete);
	}

	public sealed class CheckResult
	{
		public List<string> Errors { get; } = [];
		public List<string> Warnings { get; } = [];
		public int TextCount { get; set; }
		public int TranslatedCount { get; set; }
		/// <summary>
		/// The number of translations which this game data doesn't use (e.g. texts of other versions).
		/// </summary>
		public int UnusedCount { get; set; }
		public Dictionary<string, (int Total, int Translated)> Categories { get; } = [];
	}

	static string Category(string context) => context switch
	{
		TranslationExtractor.WordContext => "words",
		_ when context.StartsWith("name/") => context,
		_ => context.Split('/')[0]
	};

	static string Short(string text)
	{
		text = text.Replace("\n", "\\n");

		return text.Length > 60 ? text[..57] + "..." : text;
	}

	[GeneratedRegex(@"\{[0-9A-Fa-f]{2}\}")]
	private static partial Regex ControlCodeRegex();

	[GeneratedRegex(@"\{\d+\}")]
	private static partial Regex PlaceholderRegex();

	/// <summary>
	/// Checks a translation for the game data of the given folder (the language it was made from).
	/// </summary>
	public static CheckResult Check(string folder, PoFile translation)
	{
		var result = new CheckResult();
		var original = LoadGameData(folder);

		if (original.HasTranslation)
			throw new InvalidOperationException($"The game data of {folder} is already translated. Use the folder of the original game data.");

		var language = DetectLanguage(original);
		var sourceLanguage = translation.GetHeader("X-Source-Language");

		if (sourceLanguage != null && sourceLanguage != language)
			result.Errors.Add($"The translation was made from the {sourceLanguage} texts, but the game data is {language}.");

		var units = TranslationExtractor.Extract(original);
		var translations = translation.Entries.Where(e => e.IsTranslated).GroupBy(e => (e.Context, e.Id)).ToDictionary(g => g.Key, g => g.First().Translation);
		var textTranslations = translation.Entries
			.Where(e => e.IsTranslated && e.Context != TranslationExtractor.WordContext && !e.Context.StartsWith("name/") && !e.Context.StartsWith(ProgramContextPrefix))
			.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First().Translation);
		var usedEntries = new HashSet<(string, string)>();
		var counted = new HashSet<(string, string)>();
		var keywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		string? Lookup(TranslationUnit unit)
		{
			if (translations.TryGetValue((unit.Context, unit.Source), out var text))
			{
				usedEntries.Add((unit.Context, unit.Source));
				return text;
			}

			return unit.Context != TranslationExtractor.WordContext && !unit.Context.StartsWith("name/")
				? textTranslations.GetValueOrDefault(unit.Source)
				: null;
		}

		foreach (var unit in units)
		{
			if (!counted.Add((unit.Context, unit.Source)))
				continue;

			var translated = Lookup(unit);
			var category = Category(unit.Context);
			var (total, translatedCount) = result.Categories.GetValueOrDefault(category);
			result.Categories[category] = (total + 1, translatedCount + (translated == null ? 0 : 1));
			result.TextCount++;

			if (translated == null)
				continue;

			result.TranslatedCount++;
			string where = $"{unit.Context} \"{Short(unit.Source)}\"";

			if (unit.MaxLength != 0 && translated.Length > unit.MaxLength)
				result.Errors.Add($"{where}: the translation \"{translated}\" is longer than {unit.MaxLength} characters.");

			if (unit.Context == TranslationExtractor.WordContext && unit.Note.Contains("player types it"))
			{
				var keyword = translated.Trim();

				if (keyword.Contains(' ') && !unit.Source.Contains(' '))
					result.Warnings.Add($"{where}: the keyword \"{keyword}\" has a space, but the player types a single word.");

				if (keywords.TryGetValue(keyword, out var otherSource) && otherSource != unit.Source)
					result.Warnings.Add($"{where}: the keyword \"{keyword}\" is also the translation of \"{otherSource}\" (only one of them can be typed).");
				else
					keywords[keyword] = unit.Source;
			}

			if (unit.Context.StartsWith("ui/"))
			{
				var sourceCodes = string.Concat(ControlCodeRegex().Matches(unit.Source).Select(m => m.Value.ToUpperInvariant()));
				var translatedCodes = string.Concat(ControlCodeRegex().Matches(translated).Select(m => m.Value.ToUpperInvariant()));

				if (sourceCodes != translatedCodes)
					result.Errors.Add($"{where}: the control codes ({sourceCodes}) were changed ({translatedCodes}).");

				if (GameCharset.ToGameString(unit.Source).Length != GameCharset.ToGameString(translated).Length)
					result.Warnings.Add($"{where}: the translation has another length (the spaces are used for the layout).");
			}
			else if (unit.Context.StartsWith("outro/"))
			{
				int maxLineLength = unit.Source.Split('\n').Max(line => line.Length);

				if (translated.Replace("\r\n", "\n").Split('\n').Any(line => line.Length > maxLineLength))
					result.Warnings.Add($"{where}: a line is longer than {maxLineLength} characters.");
			}
			else if (unit.Context != TranslationExtractor.WordContext && !unit.Context.StartsWith("name/"))
			{
				foreach (var word in translated.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries))
				{
					if (word.Length > MaxWordLength)
						result.Warnings.Add($"{where}: the word \"{word}\" is too long to fit in a line.");
				}
			}
		}

		foreach (var (key, text) in ProgramTexts.Texts)
		{
			var source = language == "German" ? text.German : text.English;
			var context = ProgramContextPrefix + key;
			var category = "program";
			var translated = translations.GetValueOrDefault((context, source));
			var (total, translatedCount) = result.Categories.GetValueOrDefault(category);
			result.Categories[category] = (total + 1, translatedCount + (translated == null ? 0 : 1));
			result.TextCount++;

			if (translated == null)
				continue;

			usedEntries.Add((context, source));
			result.TranslatedCount++;

			var sourcePlaceholders = string.Join(",", PlaceholderRegex().Matches(source).Select(m => m.Value).Order());
			var translatedPlaceholders = string.Join(",", PlaceholderRegex().Matches(translated).Select(m => m.Value).Order());

			if (sourcePlaceholders != translatedPlaceholders)
				result.Errors.Add($"{context}: the translation must have the placeholders {sourcePlaceholders} (it has {translatedPlaceholders}).");
		}

		// texts of other versions (e.g. the Amiga version has some other words than the DOS version) or changed texts
		result.UnusedCount = translation.Entries.Count(e => e.IsTranslated && !usedEntries.Contains((e.Context, e.Id)) &&
			!counted.Contains((e.Context, e.Id)) && !textTranslations.ContainsKey(e.Id));

		// Load all translated texts like the game does.
		var translatedData = LoadGameData(folder, translation);
		List<TranslationUnit> translatedUnits;

		try
		{
			translatedUnits = TranslationExtractor.Extract(translatedData);
		}
		catch (Exception ex)
		{
			result.Errors.Add($"The translated texts could not be loaded: {ex.Message}");
			return result;
		}

		foreach (var ch in translatedData.InvalidTranslationCharacters.Order())
			result.Errors.Add($"The character '{ch}' (U+{(int)ch:X4}) is not part of the character set of the game.");

		var font = translatedData.FontLoader.LoadFont();
		var missingGlyphs = new SortedSet<string>();
		// Some characters of the original texts have no glyph (e.g. the paper code of the dialog title).
		var originalMissingGlyphs = units
			.Where(u => !u.Context.StartsWith("outro/"))
			.SelectMany(u => GameCharset.GetCharactersWithoutGlyph(GameCharset.ToGameString(u.Source), font))
			.ToHashSet();

		foreach (var unit in translatedUnits.Where(u => !u.Context.StartsWith("outro/")))
		{
			foreach (var ch in GameCharset.GetCharactersWithoutGlyph(GameCharset.ToGameString(unit.Source), font))
				missingGlyphs.Add(ch);
		}

		foreach (var (key, _) in ProgramTexts.Texts)
		{
			if (translatedData.TextLoader.GetProgramText(key.ToString()) is string text)
			{
				foreach (var ch in GameCharset.GetCharactersWithoutGlyph(PlaceholderRegex().Replace(text, ""), font))
					missingGlyphs.Add(ch);
			}
		}

		foreach (var ch in missingGlyphs.Where(ch => !originalMissingGlyphs.Contains(ch)))
			result.Errors.Add($"The font of the game has no glyph for the character '{ch}'.");

		if (translatedData.OutroData is IOutroData outro)
		{
			var originalCharacters = original.OutroData?.UsedCharacters.ToHashSet() ?? [];

			foreach (var ch in outro.UsedCharacters.Where(ch => ch > ' ' && !originalCharacters.Contains(ch) && outro.GetGlyph(ch) == null).Order())
				result.Errors.Add($"The font of the outro has no glyph for the character '{ch}'.");
		}

		return result;
	}

	/// <summary>
	/// Creates a folder with the game data and the translation. It can be packed with Amberstar.VersionPacker
	/// or used with the option --data of the game.
	/// </summary>
	public static void Install(string folder, PoFile translation, string outputFolder)
	{
		if (Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) == Path.GetFullPath(outputFolder).TrimEnd(Path.DirectorySeparatorChar))
			throw new InvalidOperationException("The output folder must not be the folder of the original game data.");

		Directory.CreateDirectory(outputFolder);

		foreach (var file in Directory.GetFiles(folder))
		{
			if (!Path.GetFileName(file).Equals(TranslationFileName, StringComparison.OrdinalIgnoreCase))
				File.Copy(file, Path.Combine(outputFolder, Path.GetFileName(file)), true);
		}

		translation.Save(Path.Combine(outputFolder, TranslationFileName));
	}
}
