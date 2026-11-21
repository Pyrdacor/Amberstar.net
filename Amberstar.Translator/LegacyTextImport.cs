using System.Text;
using System.Text.RegularExpressions;
using Amberstar.GameData;
using Amberstar.GameData.Legacy.Translation;

namespace Amberstar.Translator;

/// <summary>
/// Imports translated texts in the folder format of the text export of the Amberstar tool (a folder per kind of
/// text with a file per text, e.g. ClassNames/001.txt, MapTexts/004/000.txt and UITexts/009.txt).
///
/// The texts are the words of the dictionary: '#' is a line break and '$' is a new paragraph. The UI texts have
/// the control codes as characters.
/// </summary>
public static partial class LegacyTextImport
{
	public sealed record Result(int Imported, int Replaced, int Unchanged, int Suggested, List<string> Unknown, List<string> Conflicts);

	// folder -> location prefix of the single words (see TranslationExtractor)
	static readonly Dictionary<string, string> wordFolders = new()
	{
		{ "ClassNames", "ClassName" },
		{ "RaceNames", "RaceName" },
		{ "AttributeNames", "AttributeName" },
		{ "SkillNames", "SkillName" },
		{ "CharInfoTexts", "CharInfoText" },
		{ "LanguageNames", "LanguageName" },
		{ "ConditionNames", "ConditionName" },
		{ "ItemTypeNames", "ItemTypeName" },
		{ "SpellSchoolNames", "SpellSchoolName" },
		{ "SpellNames", "SpellName" },
	};

	// folder -> context of the text blocks
	static readonly Dictionary<string, string> textFolders = new()
	{
		{ "MapTexts", "maptext" },
		{ "ItemTexts", "itemtext" },
		{ "PuzzleTexts", "puzzletext" },
		{ "Messages", "message" },
	};

	[GeneratedRegex(@"[ \t]+")]
	private static partial Regex SpacesRegex();

	/// <summary>
	/// Converts a text with the markers of the dictionary to the format of the translations.
	/// </summary>
	public static string ConvertText(string text)
	{
		text = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("…", "...");
		var builder = new StringBuilder();

		foreach (var line in text.Split('\n'))
		{
			if (builder.Length != 0)
				builder.Append('\n');

			bool space = false;

			foreach (var word in line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
			{
				if (word == "#")
				{
					builder.Append('\n');
					space = false;
				}
				else if (word == "$")
				{
					builder.Append("{P}");
					space = false;
				}
				else
				{
					if (space)
						builder.Append(' ');

					builder.Append(GameCharset.ToText(word));
					space = true;
				}
			}
		}

		return builder.ToString().TrimEnd('\n');
	}

	/// <summary>
	/// Puts the translations of the folder into the PO file. Existing translations which are fuzzy (e.g. the ones of
	/// Ambermoon) are replaced, confirmed ones are kept.
	/// </summary>
	public static Result Import(PoFile translation, string folder)
	{
		var entriesByLocation = new Dictionary<string, PoEntry>();

		foreach (var entry in translation.Entries)
		{
			foreach (var reference in entry.References)
				entriesByLocation.TryAdd(reference, entry);
		}

		var imported = new Dictionary<PoEntry, string>();
		var unknown = new List<string>();
		var conflicts = new List<string>();
		int unchanged = 0;

		foreach (var file in Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
		{
			var path = Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');
			var parts = path[..^4].Split('/');
			string? location = null;
			bool rawText = false;

			if (parts.Length == 2 && wordFolders.TryGetValue(parts[0], out var prefix) && int.TryParse(parts[1], out int index))
				location = $"{prefix}/{index:000}";
			else if (parts.Length == 2 && parts[0] == "UITexts" && int.TryParse(parts[1], out index) && Enum.IsDefined((UIText)index))
			{
				location = $"ui/{(UIText)index}";
				rawText = true;
			}
			else if (textFolders.TryGetValue(parts[0], out var context))
			{
				// one file per message, a folder per container of the other texts (PuzzleTexts has a single container)
				if (parts.Length == 2 && context == "message" && int.TryParse(parts[1], out index))
					location = $"message/{index:000}";
				else if (parts.Length == 2 && int.TryParse(parts[1], out index))
					location = $"{context}/001/{index:000}";
				else if (parts.Length == 3 && int.TryParse(parts[1], out int container) && int.TryParse(parts[2], out index))
					location = $"{context}/{container:000}/{index:000}";
			}

			// some files have more than one byte order mark
			var content = File.ReadAllText(file, Encoding.UTF8).Replace("\uFEFF", "");

			// unused places (e.g. of spells)
			if (content.Trim().Length == 0)
				continue;

			if (location == null || !entriesByLocation.TryGetValue(location, out var entry))
			{
				unknown.Add(path);
				continue;
			}

			var text = rawText ? GameCharset.ToText(content.TrimEnd('\r', '\n')) : ConvertText(content.Trim());

			if (text.Trim().Length == 0)
				continue;

			if (imported.TryGetValue(entry, out var other))
			{
				if (other != text)
					conflicts.Add($"{path}: \"{text}\" (\"{entry.Id}\" is already \"{other}\")");

				continue;
			}

			if (text == entry.Id)
				unchanged++;

			imported.Add(entry, text);
		}

		int replaced = 0, suggested = 0;

		foreach (var (entry, text) in imported)
		{
			if (entry.IsTranslated && entry.Translation != text)
			{
				conflicts.Add($"{entry.Context} \"{entry.Id}\": kept the existing translation \"{entry.Translation}\" (imported \"{text}\")");
				continue;
			}

			if (entry.Translation.Length != 0)
				replaced++;

			entry.Translation = text;
			entry.Flags.Remove("fuzzy");
			entry.TranslatorComments.RemoveAll(c => c.StartsWith("From the Ambermoon translation") || c.StartsWith("Ambermoon German:"));
		}

		// The same English words as the names (e.g. a class name which is also a keyword) get the translation as a
		// suggestion.
		var nameTranslations = imported
			.Where(e => TranslationExtractor.IsNameOfProgramData(e.Key.Context))
			.GroupBy(e => e.Key.Id)
			.ToDictionary(g => g.Key, g => g.First());

		foreach (var entry in translation.Entries.Where(e => e.Context == TranslationExtractor.WordContext && e.Translation.Length == 0))
		{
			if (nameTranslations.TryGetValue(entry.Id, out var name))
			{
				entry.Translation = name.Value;
				entry.Flags.Add("fuzzy");
				entry.TranslatorComments.Add($"The translation of {name.Key.Context}. Check it and remove the fuzzy flag.");
				suggested++;
			}
		}

		return new(imported.Count, replaced, unchanged, suggested, unknown, conflicts);
	}
}
