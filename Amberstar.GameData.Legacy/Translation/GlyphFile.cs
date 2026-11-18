using System.Text;

namespace Amberstar.GameData.Legacy.Translation;

/// <summary>
/// A character of a translation which the fonts of the game don't have.
/// </summary>
public sealed class GlyphDefinition
{
	public const int Width = 8;
	public const int Height = 5;

	public char Character { get; init; }

	/// <summary>
	/// The character whose glyph is shown (e.g. the Latin A for the Cyrillic А), or null if <see cref="Rows"/> are given.
	/// </summary>
	public char? Reference { get; init; }

	/// <summary>
	/// The pixels of the glyph: <see cref="Height"/> rows of up to <see cref="Width"/> characters ('#' is set).
	/// </summary>
	public string[]? Rows { get; init; }

	/// <summary>
	/// Comment lines in front of the definition.
	/// </summary>
	public List<string> Comments { get; } = [];

	/// <summary>
	/// The glyph in the format of the fonts of the game (one byte per row, the highest bit is the left pixel).
	/// </summary>
	public byte[] GetRowBytes()
	{
		var bytes = new byte[Height];

		for (int y = 0; y < Height && Rows != null && y < Rows.Length; y++)
		{
			for (int x = 0; x < Width && x < Rows[y].Length; x++)
			{
				if (Rows[y][x] == '#')
					bytes[y] |= (byte)(0x80 >> x);
			}
		}

		return bytes;
	}
}

/// <summary>
/// The glyphs of the characters of a translation which the fonts of the game don't have (file GLYPHS.TXT
/// next to the translation). A character can show the glyph of another character or have its own glyph
/// of 5 rows ('#' is a set pixel, '.' is not set). The glyphs of the original are 5 pixels wide and the
/// special characters have the letter in the lower 4 rows (e.g. the Ä is the A with the dots in the first row).
/// Lowercase letters use the glyph of the uppercase letter if they are not given (the fonts have no lowercase letters).
///
/// <code>
/// # comment
/// А = A
/// É
/// ..##.
/// #####
/// ##...
/// ####.
/// #####
/// </code>
/// </summary>
public sealed class GlyphFile
{
	public const string FileName = "GLYPHS.TXT";

	public List<GlyphDefinition> Glyphs { get; } = [];

	public GlyphDefinition? Find(char character) => Glyphs.FirstOrDefault(glyph => glyph.Character == character);

	public static GlyphFile Load(string path) => Parse(File.ReadAllText(path, Encoding.UTF8));

	public void Save(string path) => File.WriteAllText(path, Write(), new UTF8Encoding(false));

	static bool IsPixelRow(string line) => line.Length != 0 && line.Length <= GlyphDefinition.Width && line.All(ch => ch is '#' or '.');

	public static GlyphFile Parse(string content)
	{
		var file = new GlyphFile();
		var lines = content.Replace("\r\n", "\n").Split('\n');
		var comments = new List<string>();

		for (int i = 0; i < lines.Length; i++)
		{
			var line = lines[i].Trim();

			if (line.Length == 0)
				continue;

			if (line[0] == '#' && !IsPixelRow(line))
			{
				comments.Add(line[1..].Trim());
				continue;
			}

			if (line.Length >= 3 && line[1] == ' ' || line.Length >= 2 && line[1] == '=')
			{
				// X = Y
				var parts = line.Split('=', 2);

				if (parts.Length != 2 || parts[0].Trim().Length != 1 || parts[1].Trim().Length != 1)
					throw new FormatException($"{FileName} line {i + 1}: invalid reference '{line}' (expected e.g. 'А = A').");

				file.Add(new GlyphDefinition { Character = parts[0].Trim()[0], Reference = parts[1].Trim()[0] }, comments, i);
				continue;
			}

			if (line.Length != 1)
				throw new FormatException($"{FileName} line {i + 1}: expected a character, a reference like 'А = A' or a comment.");

			var rows = new string[GlyphDefinition.Height];

			for (int y = 0; y < rows.Length; y++)
			{
				if (++i >= lines.Length || !IsPixelRow(lines[i].Trim()))
					throw new FormatException($"{FileName} line {i + 1}: expected {GlyphDefinition.Height} rows with up to {GlyphDefinition.Width} pixels ('#' or '.') for '{line}'.");

				rows[y] = lines[i].Trim();
			}

			file.Add(new GlyphDefinition { Character = line[0], Rows = rows }, comments, i);
		}

		return file;
	}

	void Add(GlyphDefinition glyph, List<string> comments, int lineIndex)
	{
		if (Find(glyph.Character) != null)
			throw new FormatException($"{FileName} line {lineIndex + 1}: the character '{glyph.Character}' is defined twice.");

		glyph.Comments.AddRange(comments);
		comments.Clear();
		Glyphs.Add(glyph);
	}

	public string Write()
	{
		var builder = new StringBuilder();

		foreach (var glyph in Glyphs)
		{
			foreach (var comment in glyph.Comments)
				builder.AppendLine($"# {comment}".TrimEnd());

			if (glyph.Reference != null)
				builder.AppendLine($"{glyph.Character} = {glyph.Reference}");
			else
			{
				builder.AppendLine(glyph.Character.ToString());

				foreach (var row in glyph.Rows!)
					builder.AppendLine(row);
			}

			builder.AppendLine();
		}

		return builder.ToString();
	}
}
