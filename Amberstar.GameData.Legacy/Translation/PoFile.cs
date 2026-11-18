using System.Text;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// An entry of a gettext PO file.
/// </summary>
public sealed class PoEntry
{
	/// <summary>
	/// The key of the text (msgctxt), e.g. "maptext/012/003".
	/// </summary>
	public string Context { get; set; } = "";

	/// <summary>
	/// The source text (msgid).
	/// </summary>
	public string Id { get; set; } = "";

	/// <summary>
	/// The translation (msgstr). Empty if it is not translated.
	/// </summary>
	public string Translation { get; set; } = "";

	/// <summary>
	/// Comments for the translators (lines with "#.").
	/// </summary>
	public List<string> ExtractedComments { get; } = [];

	/// <summary>
	/// Comments of the translators (lines with "# ").
	/// </summary>
	public List<string> TranslatorComments { get; } = [];

	/// <summary>
	/// The places of the text (lines with "#:").
	/// </summary>
	public List<string> References { get; } = [];

	/// <summary>
	/// Flags like "fuzzy" (lines with "#,").
	/// </summary>
	public List<string> Flags { get; } = [];

	public bool IsFuzzy => Flags.Contains("fuzzy");

	public bool IsTranslated => Translation.Length != 0 && !IsFuzzy;
}

/// <summary>
/// A gettext PO file (https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html).
/// It can be edited with tools like Poedit, Weblate or Crowdin, or with a text editor. Plural
/// forms and obsolete entries are not supported (obsolete entries are skipped).
/// </summary>
public sealed class PoFile
{
	/// <summary>
	/// The header fields (e.g. "Language" or "Content-Type"). They are stored in the entry with the empty msgid.
	/// </summary>
	public List<KeyValuePair<string, string>> Header { get; } = [];

	public List<PoEntry> Entries { get; } = [];

	public string? GetHeader(string name) => Header.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

	public void SetHeader(string name, string value)
	{
		int index = Header.FindIndex(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));

		if (index == -1)
			Header.Add(new(name, value));
		else
			Header[index] = new(name, value);
	}

	public static PoFile Load(string path) => Read(File.ReadAllText(path, Encoding.UTF8));

	public void Save(string path) => File.WriteAllText(path, Write(), new UTF8Encoding(false));

	public static PoFile Read(Stream stream)
	{
		using var reader = new StreamReader(stream, Encoding.UTF8);

		return Read(reader.ReadToEnd());
	}

	public static PoFile Read(string content)
	{
		var file = new PoFile();
		var entry = new PoEntry();
		bool hasEntryData = false;
		// the keyword which the following string lines continue
		string? currentKeyword = null;
		int lineNumber = 0;

		void Finish()
		{
			if (hasEntryData)
			{
				if (entry.Id.Length == 0 && entry.Context.Length == 0)
					file.ParseHeader(entry.Translation);
				else
					file.Entries.Add(entry);
			}

			entry = new PoEntry();
			hasEntryData = false;
			currentKeyword = null;
		}

		void Append(string keyword, string value)
		{
			switch (keyword)
			{
				case "msgctxt":
					entry.Context += value;
					break;
				case "msgid":
					entry.Id += value;
					break;
				case "msgstr":
				case "msgstr[0]":
					entry.Translation += value;
					break;
				default:
					// msgid_plural and further plural forms are not used
					break;
			}
		}

		foreach (var rawLine in content.Replace("\r\n", "\n").Split('\n'))
		{
			lineNumber++;
			var line = rawLine.Trim();

			if (line.Length == 0)
			{
				Finish();
				continue;
			}

			if (line.StartsWith("#~"))
				continue; // obsolete entry

			if (line[0] == '#')
			{
				// A comment after the strings starts a new entry.
				if (currentKeyword != null)
					Finish();

				hasEntryData = true;

				if (line.StartsWith("#."))
					entry.ExtractedComments.Add(line[2..].TrimStart());
				else if (line.StartsWith("#:"))
					entry.References.AddRange(line[2..].Split(' ', StringSplitOptions.RemoveEmptyEntries));
				else if (line.StartsWith("#,"))
					entry.Flags.AddRange(line[2..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
				else if (line.StartsWith("#|"))
					continue; // previous msgid
				else
					entry.TranslatorComments.Add(line.Length > 1 && line[1] == ' ' ? line[2..] : line[1..]);

				continue;
			}

			if (line[0] == '"')
			{
				if (currentKeyword == null)
					throw new FormatException($"Line {lineNumber}: string without keyword.");

				Append(currentKeyword, Unescape(line, lineNumber));
				continue;
			}

			int space = line.IndexOf(' ');

			if (space == -1)
				throw new FormatException($"Line {lineNumber}: invalid line '{line}'.");

			string keyword = line[..space];

			// A new msgctxt or a msgid without context starts a new entry.
			if ((keyword == "msgctxt" || (keyword == "msgid" && currentKeyword != "msgctxt")) && currentKeyword != null)
				Finish();

			hasEntryData = true;
			currentKeyword = keyword;
			Append(keyword, Unescape(line[(space + 1)..].Trim(), lineNumber));
		}

		Finish();

		return file;
	}

	void ParseHeader(string header)
	{
		foreach (var line in header.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			int colon = line.IndexOf(':');

			if (colon > 0)
				Header.Add(new(line[..colon].Trim(), line[(colon + 1)..].Trim()));
		}
	}

	public string Write()
	{
		var builder = new StringBuilder();

		builder.AppendLine("msgid \"\"");
		builder.AppendLine("msgstr \"\"");

		foreach (var (key, value) in Header)
			builder.AppendLine(Escape($"{key}: {value}\n"));

		foreach (var entry in Entries)
		{
			builder.AppendLine();

			foreach (var comment in entry.TranslatorComments)
				builder.AppendLine(comment.Length == 0 ? "#" : $"# {comment}");

			foreach (var comment in entry.ExtractedComments)
				builder.AppendLine($"#. {comment}");

			if (entry.References.Count != 0)
				builder.AppendLine($"#: {string.Join(' ', entry.References)}");

			if (entry.Flags.Count != 0)
				builder.AppendLine($"#, {string.Join(", ", entry.Flags)}");

			WriteString(builder, "msgctxt", entry.Context);
			WriteString(builder, "msgid", entry.Id);
			WriteString(builder, "msgstr", entry.Translation);
		}

		return builder.ToString();
	}

	static void WriteString(StringBuilder builder, string keyword, string value)
	{
		// Multi-line texts are written with one line per text line, like the gettext tools do.
		int newLine = value.IndexOf('\n');

		if (newLine == -1 || newLine == value.Length - 1)
		{
			builder.AppendLine($"{keyword} {Escape(value)}");
			return;
		}

		builder.AppendLine($"{keyword} \"\"");

		int start = 0;

		while (start < value.Length)
		{
			int end = value.IndexOf('\n', start);
			end = end == -1 ? value.Length : end + 1;
			builder.AppendLine(Escape(value[start..end]));
			start = end;
		}
	}

	static string Escape(string value)
	{
		var builder = new StringBuilder(value.Length + 2);
		builder.Append('"');

		foreach (char ch in value)
		{
			switch (ch)
			{
				case '"': builder.Append("\\\""); break;
				case '\\': builder.Append("\\\\"); break;
				case '\n': builder.Append("\\n"); break;
				case '\t': builder.Append("\\t"); break;
				case '\r': builder.Append("\\r"); break;
				default: builder.Append(ch); break;
			}
		}

		builder.Append('"');

		return builder.ToString();
	}

	static string Unescape(string quoted, int lineNumber)
	{
		if (quoted.Length < 2 || quoted[0] != '"' || quoted[^1] != '"')
			throw new FormatException($"Line {lineNumber}: invalid string {quoted}.");

		var builder = new StringBuilder(quoted.Length);

		for (int i = 1; i < quoted.Length - 1; i++)
		{
			char ch = quoted[i];

			if (ch != '\\')
			{
				builder.Append(ch);
				continue;
			}

			if (++i >= quoted.Length - 1)
				throw new FormatException($"Line {lineNumber}: invalid escape sequence.");

			builder.Append(quoted[i] switch
			{
				'n' => '\n',
				't' => '\t',
				'r' => '\r',
				'"' => '"',
				'\\' => '\\',
				'a' => '\a',
				'b' => '\b',
				'f' => '\f',
				'v' => '\v',
				_ => throw new FormatException($"Line {lineNumber}: unknown escape sequence \\{quoted[i]}.")
			});
		}

		return builder.ToString();
	}
}
