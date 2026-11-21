using System.Text;
using System.Text.RegularExpressions;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Legacy.Translation;

namespace Amberstar.Translator;

/// <summary>
/// Creates drafts of the glyphs which a translation needs (see <see cref="GlyphFile"/>).
/// </summary>
public static partial class GlyphDrafts
{
	// Letters of other alphabets which look like the Latin letters.
	static readonly Dictionary<char, char> lookAlikes = new()
	{
		// Cyrillic
		{ 'А', 'A' }, { 'В', 'B' }, { 'Е', 'E' }, { 'К', 'K' }, { 'М', 'M' }, { 'Н', 'H' }, { 'О', 'O' }, { 'Р', 'P' },
		{ 'С', 'C' }, { 'Т', 'T' }, { 'Х', 'X' }, { 'Ѕ', 'S' }, { 'І', 'I' }, { 'Ј', 'J' }, { 'Ё', 'Ë' }, { 'Ї', 'Ï' },
		// Greek
		{ 'Α', 'A' }, { 'Β', 'B' }, { 'Ε', 'E' }, { 'Ζ', 'Z' }, { 'Η', 'H' }, { 'Ι', 'I' }, { 'Κ', 'K' }, { 'Μ', 'M' },
		{ 'Ν', 'N' }, { 'Ο', 'O' }, { 'Ρ', 'P' }, { 'Τ', 'T' }, { 'Υ', 'Y' }, { 'Χ', 'X' },
		// typography
		{ '‘', '\'' }, { '’', '\'' }, { '‚', ',' }, { '“', '"' }, { '”', '"' }, { '„', '"' }, { '–', '-' }, { '—', '-' },
		{ '…', '.' },
	};

	// The diacritical marks in one row of 5 pixels (like the dots of the umlauts of the original).
	static readonly Dictionary<char, (string Row, bool Below, string Name)> marks = new()
	{
		{ '\u0300', (".##..", false, "grave") },
		{ '\u0301', ("..##.", false, "acute") },
		{ '\u0302', (".#.#.", false, "circumflex") },
		{ '\u0303', (".####", false, "tilde") },
		{ '\u0304', ("#####", false, "macron") },
		{ '\u0306', ("#.#.#", false, "breve") },
		{ '\u0307', ("..#..", false, "dot above") },
		{ '\u0308', ("##.##", false, "diaeresis") },
		{ '\u030A', ("..#..", false, "ring above") },
		{ '\u030B', ("#..#.", false, "double acute") },
		{ '\u030C', ("#...#", false, "caron") },
		{ '\u0327', ("..##.", true, "cedilla") },
		{ '\u0328', ("...##", true, "ogonek") },
	};

	// Letters without decomposition.
	static readonly Dictionary<char, string[]> letters = new()
	{
		{ 'Ł', ["##...", "##...", "###..", "##...", "#####"] },
		{ 'Ø', [".####", "##.##", "#.#.#", "##.##", "####."] },
		{ 'Đ', ["####.", "##.##", "###.#", "##.##", "####."] },
		{ 'Ð', ["####.", "##.##", "###.#", "##.##", "####."] },
		{ 'Æ', [".####", "##.#.", "#####", "##.#.", "##.##"] },
		{ 'Œ', [".####", "##.#.", "##.##", "##.#.", ".####"] },
		{ 'Þ', ["##...", "####.", "##.##", "####.", "##..."] },
		{ 'ẞ', ["####.", "##.##", "##.#.", "##.##", "##.#."] },
		{ '¿', ["..##.", ".....", "..##.", "##...", ".###."] },
		{ '¡', ["..##.", ".....", "..##.", "..##.", "..##."] },
		{ '«', ["..#.#", ".#.#.", "#.#..", ".#.#.", "..#.#"] },
		{ '»', ["#.#..", ".#.#.", "..#.#", ".#.#.", "#.#.."] },
		// Cyrillic
		{ 'Б', ["#####", "##...", "####.", "##.##", "####."] },
		{ 'Г', ["#####", "##...", "##...", "##...", "##..."] },
		{ 'Д', [".####", ".##.#", ".##.#", "#####", "#...#"] },
		{ 'Ж', ["#.#.#", "#.#.#", ".###.", "#.#.#", "#.#.#"] },
		{ 'З', ["####.", "...##", ".###.", "...##", "####."] },
		{ 'Л', [".####", ".##.#", ".##.#", ".##.#", "##..#"] },
		{ 'П', ["#####", "##.##", "##.##", "##.##", "##.##"] },
		{ 'У', ["##.##", "##.##", ".####", "...##", "####."] },
		{ 'Ф', [".###.", "#.#.#", "#.#.#", ".###.", "..#.."] },
		{ 'Ц', ["##.##", "##.##", "##.##", "#####", "....#"] },
		{ 'Ч', ["##.##", "##.##", ".####", "...##", "...##"] },
		{ 'Ш', ["#.#.#", "#.#.#", "#.#.#", "#.#.#", "#####"] },
		{ 'Щ', ["#.#.#", "#.#.#", "#.#.#", "#####", "....#"] },
		{ 'Ъ', ["##...", ".#...", ".###.", ".#..#", ".###."] },
		{ 'Ы', ["#...#", "#...#", "###.#", "#.#.#", "###.#"] },
		{ 'Ь', ["##...", "##...", "####.", "##.##", "####."] },
		{ 'Э', ["####.", "...##", ".####", "...##", "####."] },
		{ 'Ю', ["#.##.", "#.#.#", "###.#", "#.#.#", "#.##."] },
	};

	// Letters which are mirrored Latin letters (and an optional mark).
	static readonly Dictionary<char, (char Letter, char? Mark)> mirrored = new()
	{
		{ 'И', ('N', null) },
		{ 'Й', ('N', '\u0306') },
		{ 'Я', ('R', null) },
	};

	[GeneratedRegex(@"\{[0-9A-Fa-f]{2}\}|\{\d+\}|\{P\}")]
	private static partial Regex CodeRegex();

	/// <summary>
	/// The characters of the translations which the fonts of the game (or the given glyph file) can't show.
	/// Lowercase letters are given as uppercase letters (they use the glyph of the uppercase letter).
	/// </summary>
	public static SortedSet<char> FindMissingCharacters(AssetProvider original, PoFile translation, GlyphFile? glyphs)
	{
		var font = original.FontLoader.LoadFont();
		var outro = original.OutroData;
		var missing = new SortedSet<char>();
		// characters of the source texts (e.g. ~ switches to the runes)
		var sourceCharacters = translation.Entries.SelectMany(e => e.Id).ToHashSet();

		foreach (var entry in translation.Entries.Where(e => e.Translation.Length != 0))
		{
			bool outroText = entry.Context.StartsWith("outro/");

			foreach (var ch in CodeRegex().Replace(entry.Translation, ""))
			{
				if (ch <= ' ' || ch == '\u00a0' || ch == '\uFEFF' || sourceCharacters.Contains(ch))
					continue;

				var upper = char.ToUpperInvariant(ch);

				if (glyphs?.Find(ch) != null || glyphs?.Find(upper) != null)
					continue;

				var invalid = new HashSet<char>();
				var gameString = GameCharset.ToGameString(upper.ToString(), invalid);
				bool hasGlyph = invalid.Count == 0 && gameString.Length == 1 &&
					(outroText ? outro?.GetGlyph(upper) != null : font.GetGlyph(FontCharacters.ToFontCharacter(gameString[0]), false) != null);

				if (!hasGlyph)
					missing.Add(upper);
			}
		}

		return missing;
	}

	/// <summary>
	/// Adds drafts for the missing characters to the glyph file (a new one if it is null).
	/// </summary>
	public static (GlyphFile File, int Drafts, int Empty) AddDrafts(AssetProvider original, IEnumerable<char> characters, GlyphFile? glyphs)
	{
		var font = original.FontLoader.LoadFont();
		var file = glyphs ?? new GlyphFile();
		int drafts = 0, empty = 0;

		string[]? GetRows(char ch)
		{
			var glyph = font.GetGlyph(FontCharacters.ToFontCharacter(ch), false);

			if (glyph == null)
				return null;

			var data = glyph.GetData();
			var rows = new string[GlyphDefinition.Height];

			for (int y = 0; y < rows.Length; y++)
			{
				var row = new StringBuilder();

				for (int x = 0; x < 5 && x < glyph.Width; x++)
					row.Append(data[y * glyph.Width + x] != 0 ? '#' : '.');

				rows[y] = row.ToString();
			}

			return rows;
		}

		var queue = new Queue<char>(characters);

		while (queue.Count != 0)
		{
			var ch = queue.Dequeue();

			if (file.Find(ch) != null)
				continue;

			if (lookAlikes.TryGetValue(ch, out var lookAlike))
			{
				var reference = new GlyphDefinition { Character = ch, Reference = lookAlike };
				reference.Comments.Add($"{ch} looks like {lookAlike}.");
				file.Glyphs.Add(reference);
				drafts++;

				// e.g. Ё = Ë needs the glyph of Ë
				if (GetRows(lookAlike) == null)
					queue.Enqueue(lookAlike);

				continue;
			}

			string[]? rows = null;
			string? origin = null;

			if (letters.TryGetValue(ch, out var letter))
			{
				rows = letter;
				origin = "drawn";
			}
			else if (mirrored.TryGetValue(ch, out var mirror) && GetRows(mirror.Letter) is string[] letterRows)
			{
				rows = [.. letterRows.Select(row => new string([.. row.Reverse()]))];
				origin = $"mirrored {mirror.Letter}";

				if (mirror.Mark is char markCharacter)
				{
					var mark = marks[markCharacter];
					rows = AddMark(rows, mark.Row, mark.Below);
					origin += $" with {mark.Name}";
				}
			}
			else
			{
				var decomposed = ch.ToString().Normalize(NormalizationForm.FormD);

				if (decomposed.Length == 2 && marks.TryGetValue(decomposed[1], out var mark) && GetRows(decomposed[0]) is string[] baseRows)
				{
					rows = AddMark(baseRows, mark.Row, mark.Below);
					origin = $"{decomposed[0]} with {mark.Name}";
				}
			}

			var definition = new GlyphDefinition { Character = ch, Rows = rows ?? [".....", ".....", ".....", ".....", "....."] };

			if (rows == null)
			{
				definition.Comments.Add($"TODO: draw {ch} (U+{(int)ch:X4}).");
				empty++;
			}
			else
			{
				definition.Comments.Add($"Draft of {ch} ({origin}), please check it.");
				drafts++;
			}

			file.Glyphs.Add(definition);
		}

		return (file, drafts, empty);
	}

	/// <summary>
	/// The letter in 4 rows and the mark in the other row (like the umlauts of the original: a row which is the
	/// same as its neighbor is removed, the one nearest to the middle).
	/// </summary>
	static string[] AddMark(string[] rows, string mark, bool below)
	{
		int remove = -1;

		for (int y = 0; y < rows.Length - 1; y++)
		{
			if (rows[y] == rows[y + 1] && (remove == -1 || Math.Abs(y + 1 - 2) < Math.Abs(remove - 2)))
				remove = y + 1;
		}

		if (remove == -1)
			remove = 3;

		var letter = rows.Where((_, index) => index != remove).ToList();

		if (below)
			letter.Add(mark);
		else
			letter.Insert(0, mark);

		return [.. letter];
	}
}
