using System.Text.RegularExpressions;
using Amberstar.GameData.Legacy.Translation;

namespace Amberstar.Translator;

/// <summary>
/// Takes translations of the Ambermoon translations for the same English texts (e.g. the names of the classes,
/// races, skills, conditions, items and spells, some UI texts and messages).
///
/// The texts of Ambermoon are the text exports of the translators (folder AllTexts with a text file per text,
/// e.g. Text.amb/ClassNames/001.txt). The English and the translated export have the same files.
/// </summary>
public static partial class AmbermoonImport
{
	public sealed record Result(int Imported, Dictionary<string, int> Categories);

	sealed record Candidate(string Translation, string Category, string Path);

	[GeneratedRegex(@"\{[0-9A-F]{2}\}")]
	private static partial Regex ControlCodeRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex SpaceRegex();

	// The categories of Ambermoon which fit the places of the Amberstar texts (the same English word can be
	// another thing, e.g. STONED is a condition in Amberstar and a spell in Ambermoon).
	static readonly (string Reference, string[] Categories)[] preferredCategories =
	[
		("ClassName/", ["Text.amb/ClassNames"]),
		("RaceName/", ["Text.amb/RaceNames"]),
		("AttributeName/", ["Text.amb/AttributeNames"]),
		("SkillName/", ["Text.amb/SkillNames"]),
		("LanguageName/", ["Text.amb/LanguageNames"]),
		("ConditionName/", ["Text.amb/ConditionNames"]),
		("ItemTypeName/", ["Text.amb/ItemTypeNames"]),
		("SpellSchoolName/", ["Text.amb/SpellClassNames"]),
		("SpellName/", ["Text.amb/SpellNames", "Objects.amb"]),
		("item/", ["Objects.amb"]),
		("character/", ["Monster_char.amb", "NPC_char.amb", "Party_texts.amb"]),
		("ui/", ["Text.amb/UITexts"]),
		("message/", ["Text.amb/Messages"]),
	];

	static string Normalize(string text) => SpaceRegex().Replace(text.Replace('^', ' '), " ").Trim().ToUpperInvariant();

	static Dictionary<string, string> LoadTexts(string folder)
	{
		var texts = new Dictionary<string, string>();

		foreach (var file in Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories))
		{
			var path = Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');

			// The dictionary and the savegame are not texts.
			if (!path.StartsWith("Dict.amb/") && !path.StartsWith("Save."))
				texts[path] = File.ReadAllText(file).Trim();
		}

		return texts;
	}

	static string Category(string path)
	{
		var parts = path.Split('/');

		return parts[0] == "Text.amb" && parts.Length > 2 ? $"{parts[0]}/{parts[1]}" : parts[0];
	}

	/// <summary>
	/// Fills the untranslated entries of the translation (or template) with the translations of Ambermoon. They
	/// are marked as fuzzy, so they are only used after a translator checked them.
	/// </summary>
	/// <param name="germanFolder">The German text export of Ambermoon (optional): if its text differs from the German
	/// text of Amberstar (comment of the template), the meaning could be another and a hint is added.</param>
	public static Result Import(PoFile translation, string englishFolder, string translatedFolder, string? germanFolder = null)
	{
		var english = LoadTexts(englishFolder);
		var translated = LoadTexts(translatedFolder);
		var german = germanFolder == null ? null : LoadTexts(germanFolder);
		var candidates = new Dictionary<string, List<Candidate>>();

		foreach (var (path, englishText) in english)
		{
			if (!translated.TryGetValue(path, out var text) || text.Length == 0 || englishText.Length == 0 ||
				text.Contains('~') || englishText.Contains('~')) // placeholders of Ambermoon
				continue;

			var key = Normalize(englishText);

			if (!candidates.TryGetValue(key, out var list))
				candidates.Add(key, list = []);

			list.Add(new(Normalize(text.Replace('$', ' ')), Category(path), path));
		}

		var categories = new Dictionary<string, int>();
		int imported = 0;

		foreach (var entry in translation.Entries)
		{
			if (entry.Translation.Length != 0 || entry.Context.StartsWith("program/") || entry.Context.StartsWith("outro/"))
				continue;

			// The UI texts keep their control codes, spaces and colons.
			string prefix = "", suffix = "", core = entry.Id;

			if (entry.Context.StartsWith("ui/"))
			{
				var match = Regex.Match(entry.Id, @"^((?:\{[0-9A-F]{2}\}| )*)(.*?)((?: |:|\{[0-9A-F]{2}\})*)$");
				(prefix, core, suffix) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);

				if (ControlCodeRegex().IsMatch(core))
					continue;
			}

			if (core.Length == 0 || !candidates.TryGetValue(Normalize(core.Replace('\n', ' ')), out var list))
				continue;

			var preferred = preferredCategories
				.Where(p => entry.References.Any(r => r.StartsWith(p.Reference)))
				.SelectMany(p => p.Categories)
				.ToList();
			// Another kind of text could have another meaning (e.g. a condition and a spell).
			if (preferred.Count != 0 && !list.Any(c => preferred.Contains(c.Category)))
				continue;

			var choice = list
				.GroupBy(c => c.Translation)
				.OrderByDescending(g => g.Any(c => preferred.Contains(c.Category)))
				.ThenByDescending(g => g.Count())
				.First();
			var candidate = choice.OrderByDescending(c => preferred.Contains(c.Category)).First();
			var text = candidate.Translation;

			// the same width as the English text (the UI texts are aligned with spaces)
			if (entry.Context.StartsWith("ui/") && text.Length < core.Length)
				text = text.PadRight(core.Length);

			text = prefix + text + suffix;

			int maxLength = entry.ExtractedComments
				.Select(c => Regex.Match(c, @"^Max\. (\d+) characters\.$"))
				.Where(m => m.Success)
				.Select(m => int.Parse(m.Groups[1].Value))
				.FirstOrDefault();

			if (maxLength != 0 && text.Length > maxLength)
				continue;

			entry.Translation = text;

			if (!entry.Flags.Contains("fuzzy"))
				entry.Flags.Add("fuzzy");

			entry.TranslatorComments.Add($"From the Ambermoon translation ({candidate.Path}). Check it and remove the fuzzy flag.");

			var amberstarGerman = entry.ExtractedComments.FirstOrDefault(c => c.StartsWith("German: "))?["German: ".Length..];

			if (german != null && amberstarGerman != null && german.TryGetValue(candidate.Path, out var ambermoonGerman))
			{
				static string Simplify(string text) => Normalize(ControlCodeRegex().Replace(text, "")).Trim(' ', ':');

				if (Simplify(ambermoonGerman) != Simplify(amberstarGerman))
					entry.TranslatorComments.Add($"Ambermoon German: {Normalize(ambermoonGerman)}, Amberstar German: {Simplify(amberstarGerman)} (check if it means the same).");
			}

			var category = entry.Context.StartsWith("name/") ? entry.Context : entry.Context.Split('/')[0];
			categories[category] = categories.GetValueOrDefault(category) + 1;
			imported++;
		}

		return new(imported, categories);
	}
}
