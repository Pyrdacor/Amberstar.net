using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amberstar.GameData.Serialization;

public interface ITextLoader
{
	IText LoadText(AssetIdentifier assetIdentifier);
    IText ReadText(IDataReader dataReader);
    IText FromString(string text);
    IText FromTextFragmentIndex(word index);
    word? FindWord(string word);

    /// <summary>
    /// True if the texts of the game data are translated (a translation made with Amberstar.Translator).
    /// </summary>
    bool HasTranslation => false;

    /// <summary>
    /// The language of the game data which the translation was made from (e.g. "English") or null.
    /// </summary>
    string? TranslationSourceLanguage => null;

    /// <summary>
    /// The translation of a text of the program which is not part of the game data (e.g. the entries of the
    /// main menu), or null if there is none.
    /// </summary>
    string? GetProgramText(string key) => null;

    /// <summary>
    /// Converts typed text to the characters of the game data (a translation can have characters which are
    /// stored as other characters, see the glyph file of Amberstar.Translator).
    /// </summary>
    string ToGameText(string text) => text;
}

public static class TextLoaderExtensions
{
    public static IText Concat(this ITextLoader textLoader, IText left, IText right)
    {
        return textLoader.FromString(left.GetString() + right.GetString());
    }

    public static IText Concat(this ITextLoader textLoader, string left, IText right)
    {
        return textLoader.FromString(left + right.GetString());
    }

    public static IText Concat(this ITextLoader textLoader, IText left, string right)
    {
        return textLoader.FromString(left.GetString() + right);
    }

    public static IText ConcatWithSeparator(this ITextLoader textLoader, IText left, string separator, IText right)
    {
        return textLoader.FromString(left.GetString() + separator + right.GetString());
    }
}