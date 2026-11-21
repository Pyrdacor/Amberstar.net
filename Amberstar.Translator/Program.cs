using Amberstar.GameData.Legacy.Translation;
using Amberstar.Translator;

// Extracts the texts of the game data for translators (gettext PO files) and integrates the translations.

string[] defaultFolders =
[
	Path.Combine("English", "AmberfilesAmiga"),
	Path.Combine("English", "AmberfilesST"),
	Path.Combine("English", "AmberfilesDOS"),
];
string[] defaultReferenceFolders =
[
	Path.Combine("German", "AmberfilesAmiga"),
	Path.Combine("German", "AmberfilesST"),
	Path.Combine("German", "AmberfilesDOS"),
];

try
{
	var command = args.FirstOrDefault()?.ToLowerInvariant();
	var options = ParseOptions(args.Skip(1), out var arguments);

	switch (command)
	{
		case "extract":
		{
			var output = options.GetValueOrDefault("output") ?? "amberstar.pot";
			var folders = arguments.Count != 0 ? arguments : [.. defaultFolders.Select(FindRepositoryFolder)];
			var referenceFolders = options.TryGetValue("reference", out var reference)
				? (reference.Length == 0 ? [] : reference.Split(';', StringSplitOptions.RemoveEmptyEntries))
				: arguments.Count != 0 ? [] : defaultReferenceFolders.Select(TryFindRepositoryFolder).OfType<string>().ToArray();
			var template = TranslationTool.CreateTemplate(folders, referenceFolders, Console.WriteLine);
			template.Save(output);
			Console.WriteLine($"Wrote {template.Entries.Count} texts to {output}.");
			return 0;
		}
		case "import" when arguments.Count == 2:
		{
			var translation = PoFile.Load(arguments[0]);
			var output = options.GetValueOrDefault("output") ?? arguments[0];

			if (options.TryGetValue("language", out var language))
				translation.SetHeader("Language", language);

			var result = LegacyTextImport.Import(translation, arguments[1]);
			translation.Save(output);

			foreach (var conflict in result.Conflicts)
				Console.WriteLine("Conflict: " + conflict);

			foreach (var path in result.Unknown)
				Console.WriteLine("Unknown text: " + path);

			Console.WriteLine($"Wrote {output}: {result.Imported} translations imported ({result.Replaced} replaced fuzzy ones, {result.Unchanged} are the same as the source text).");

			if (result.Suggested != 0)
				Console.WriteLine($"{result.Suggested} words with the same English text (e.g. keywords) got the translation of a name as a fuzzy suggestion.");
			return 0;
		}
		case "ambermoon" when arguments.Count == 3:
		{
			var translation = PoFile.Load(arguments[0]);
			var output = options.GetValueOrDefault("output") ?? arguments[0];

			if (options.TryGetValue("language", out var language))
				translation.SetHeader("Language", language);

			var result = AmbermoonImport.Import(translation, arguments[1], arguments[2], options.GetValueOrDefault("german"));
			translation.Save(output);
			Console.WriteLine($"Wrote {output}: {result.Imported} translations of Ambermoon (marked as fuzzy).");

			foreach (var (category, count) in result.Categories.OrderBy(c => c.Key))
				Console.WriteLine($"  {category,-16} {count,5}");

			return 0;
		}
		case "update" when arguments.Count == 2:
		{
			var (result, kept, fuzzy, obsolete) = TranslationTool.Merge(PoFile.Load(arguments[0]), PoFile.Load(arguments[1]));
			var output = options.GetValueOrDefault("output") ?? arguments[0];
			result.Save(output);
			Console.WriteLine($"Wrote {output}: {kept} translations kept, {fuzzy} marked as fuzzy (changed source text), {obsolete} dropped.");
			return 0;
		}
		case "check" when arguments.Count == 2:
			return PrintCheck(TranslationTool.Check(FindDataFolder(arguments[0]), PoFile.Load(arguments[1]), LoadGlyphs(options, arguments[1]))) ? 0 : 1;
		case "glyphs" when arguments.Count == 2:
		{
			var original = TranslationTool.LoadGameData(FindDataFolder(arguments[0]));
			var glyphs = LoadGlyphs(options, arguments[1], false);
			var output = options.GetValueOrDefault("output") ?? options.GetValueOrDefault("glyphs") ?? DefaultGlyphPath(arguments[1]);
			var missing = GlyphDrafts.FindMissingCharacters(original, PoFile.Load(arguments[1]), glyphs);

			// additional characters (e.g. all special characters of a language)
			if (options.TryGetValue("characters", out var characters))
			{
				var font = original.FontLoader.LoadFont();

				foreach (var ch in characters.Where(ch => ch > ' ').Select(char.ToUpperInvariant))
				{
					if (glyphs?.Find(ch) == null && !GameCharset.GetCharactersWithoutGlyph(GameCharset.ToGameString(ch.ToString()), font).Any() &&
						GameCharset.ToGameString(ch.ToString()).Length == 1)
						continue; // the font has it

					if (glyphs?.Find(ch) == null)
						missing.Add(ch);
				}
			}

			if (missing.Count == 0)
			{
				Console.WriteLine("The translation needs no new glyphs.");
				return 0;
			}

			var (file, drafts, empty) = GlyphDrafts.AddDrafts(original, missing, glyphs);
			file.Save(output);
			Console.WriteLine($"Characters without glyph: {string.Concat(missing)}");
			Console.WriteLine($"Wrote {output}: {drafts} drafts, {empty} empty glyphs to draw.");
			return 0;
		}
		case "create" when arguments.Count == 3:
		{
			var folder = FindDataFolder(arguments[0]);
			var translation = PoFile.Load(arguments[1]);

			var glyphs = LoadGlyphs(options, arguments[1]);

			if (!PrintCheck(TranslationTool.Check(folder, translation, glyphs)) && !options.ContainsKey("force"))
			{
				Console.WriteLine("The translation has errors. Fix them or use --force.");
				return 1;
			}

			TranslationTool.Install(folder, translation, arguments[2], glyphs);
			Console.WriteLine($"Created {arguments[2]} with the game data of {folder} and the translation.");
			Console.WriteLine("Start the game with it: Amberstar.net --data " + arguments[2]);
			return 0;
		}
	}
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or FormatException or InvalidOperationException)
{
	Console.WriteLine("Error: " + ex.Message);
	return 1;
}

Console.WriteLine("Usage:");
Console.WriteLine("  Amberstar.Translator extract [--output <file.pot>] [--reference <folder>[;<folder>...]] [<data folder>...]");
Console.WriteLine("      Writes all texts of the game data to a PO template for the translators (default amberstar.pot).");
Console.WriteLine("      Without folders the English Amiga, Atari ST and DOS data of the repository are used and the");
Console.WriteLine("      German texts are added as comments (--reference \"\" leaves them out).");
Console.WriteLine("  Amberstar.Translator update <translation.po> <template.pot> [--output <file.po>]");
Console.WriteLine("      Takes the translations for a new template (changed source texts are marked as fuzzy).");
Console.WriteLine("  Amberstar.Translator glyphs <data folder> <translation.po> [--glyphs <glyphs.txt>] [--output <glyphs.txt>] [--characters <chars>]");
Console.WriteLine("      Writes drafts of the glyphs of the characters which the fonts don't have (e.g. É or Ł) to a glyph");
Console.WriteLine("      file (default: next to the translation, e.g. cs.glyphs.txt for cs.po, an existing glyph file is extended). Check and improve them with a text editor.");
Console.WriteLine("      --characters adds further characters (e.g. all special characters of the language).");
Console.WriteLine("  Amberstar.Translator import <translation.po or template.pot> <text folder> [--output <file.po>] [--language <code>]");
Console.WriteLine("      Imports translated texts in the folder format of the text export of the Amberstar tool (ClassNames/001.txt,");
Console.WriteLine("      MapTexts/004/000.txt, ..., '#' is a line break, '$' a new paragraph). Fuzzy translations are replaced.");
Console.WriteLine("  Amberstar.Translator ambermoon <translation.po or template.pot> <Ambermoon English AllTexts> <Ambermoon translated AllTexts>");
Console.WriteLine("                               [--german <Ambermoon German AllTexts>] [--output <file.po>] [--language <code>]");
Console.WriteLine("      Fills untranslated texts with the translations of Ambermoon for the same English texts (the text exports");
Console.WriteLine("      of the Ambermoon translations). They are marked as fuzzy until a translator checked them. With the German");
Console.WriteLine("      export, texts whose German translation differs from the one of Amberstar get a hint (other meaning?).");
Console.WriteLine("  Amberstar.Translator check <data folder> <translation.po> [--glyphs <glyphs.txt>]");
Console.WriteLine("      Checks a translation (lengths, characters of the font, keywords, control codes) and shows");
Console.WriteLine("      how much is translated. The folder is the game data which the translation was made from.");
Console.WriteLine("  Amberstar.Translator create <data folder> <translation.po> <output folder> [--glyphs <glyphs.txt>] [--force]");
Console.WriteLine("      Checks the translation and creates a new game data folder with it (the files of the data");
Console.WriteLine($"      folder, the translation as {TranslationTool.TranslationFileName} and the glyphs as {GlyphFile.FileName}).");
Console.WriteLine();
Console.WriteLine("Without --glyphs, check and create use the glyph file next to the translation (e.g. cs.glyphs.txt for cs.po).");
Console.WriteLine("The data folders can also be given like English/AmberfilesAmiga (searched upwards from the current directory).");
return 1;

static Dictionary<string, string> ParseOptions(IEnumerable<string> args, out List<string> arguments)
{
	var options = new Dictionary<string, string>();
	arguments = [];
	var list = args.ToList();

	for (int i = 0; i < list.Count; i++)
	{
		var arg = list[i];

		if (arg == "--force")
			options["force"] = "";
		else if (arg is "--output" or "-o" or "--reference" or "--glyphs" or "--language" or "--german" or "--characters")
		{
			if (i + 1 >= list.Count)
				throw new ArgumentException($"The option {arg} needs a value.");

			options[arg == "-o" ? "output" : arg[2..]] = list[++i];
		}
		else if (arg.StartsWith("--"))
			throw new ArgumentException($"Unknown option {arg}.");
		else
			arguments.Add(arg);
	}

	return options;
}

// The glyph file of a translation is next to it (e.g. cs.glyphs.txt for cs.po).
static string DefaultGlyphPath(string translationPath) => Path.ChangeExtension(translationPath, ".glyphs.txt");

// The glyph file is optional for the command glyphs (it is created then).
static GlyphFile? LoadGlyphs(Dictionary<string, string> options, string translationPath, bool mustExist = true)
{
	if (!options.TryGetValue("glyphs", out var path))
	{
		var defaultPath = DefaultGlyphPath(translationPath);

		if (File.Exists(defaultPath))
		{
			Console.WriteLine($"Using the glyph file {defaultPath}.");
			return GlyphFile.Load(defaultPath);
		}

		return null;
	}

	if (File.Exists(path))
		return GlyphFile.Load(path);

	return mustExist ? throw new FileNotFoundException($"The glyph file {path} was not found.") : null;
}

static bool PrintCheck(TranslationTool.CheckResult result)
{
	foreach (var error in result.Errors)
		Console.WriteLine("Error: " + error);

	foreach (var warning in result.Warnings)
		Console.WriteLine("Warning: " + warning);

	Console.WriteLine();
	Console.WriteLine("Translated texts:");

	foreach (var (category, (total, translated)) in result.Categories.OrderBy(c => c.Key))
		Console.WriteLine($"  {category,-16} {translated,5} / {total,5}");

	Console.WriteLine($"  {"total",-16} {result.TranslatedCount,5} / {result.TextCount,5} ({(result.TextCount == 0 ? 0 : 100.0 * result.TranslatedCount / result.TextCount):0.0}%)");
	if (result.UnusedCount != 0)
		Console.WriteLine($"{result.UnusedCount} translations are not used by this game data (texts of other versions or changed texts).");

	Console.WriteLine($"{result.Errors.Count} errors, {result.Warnings.Count} warnings");

	return result.Errors.Count == 0;
}

static string FindDataFolder(string path) => Directory.Exists(path) ? path : FindRepositoryFolder(path);

static string FindRepositoryFolder(string relativePath) => TryFindRepositoryFolder(relativePath)
	?? throw new DirectoryNotFoundException($"The data folder {relativePath} was not found.");

// the data folders are searched upwards from the current directory
static string? TryFindRepositoryFolder(string relativePath)
{
	var directory = new DirectoryInfo(Environment.CurrentDirectory);

	while (directory != null)
	{
		var path = Path.Combine(directory.FullName, relativePath);

		if (Directory.Exists(path))
			return path;

		directory = directory.Parent;
	}

	return null;
}
