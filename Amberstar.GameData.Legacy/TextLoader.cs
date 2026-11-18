using Amber.Common;
using Amber.IO.Common.Serialization;
using Amberstar.GameData.Serialization;
using Amberstar.GameData.Legacy.Translation;

namespace Amberstar.GameData.Legacy;

internal class TextLoader(Amber.Assets.Common.IAssetProvider assetProvider, List<string> textFragments, GameTranslation? translation = null) : ITextLoader
{
	readonly List<string> textFragments = textFragments;

	public bool HasTranslation => translation != null;

	public string? TranslationSourceLanguage => translation?.SourceLanguage;

	public string? GetProgramText(string key) => translation?.GetProgramText(key);

	public string ToGameText(string text) => translation?.ToGameText(text) ?? text;
	readonly Dictionary<AssetIdentifier, IText> texts = [];

    public IText FromString(string text)
	{
        return Text.FromString(text);
    }

    public IText FromTextFragmentIndex(word index)
    {
        return Text.FromTextFragmentIndex(index, textFragments);
    }

    public word? FindWord(string word)
	{
		// The translated keywords are found first (another word could be the same).
		if (translation?.FindKeyword(word) is word keyword)
			return keyword;

		word = ToGameText(word);
		int index = textFragments.FindIndex(text => string.Compare(text, word, true) == 0);

		if (index == -1)
			return null;

		return (word)index;
	}

    public IText ReadText(IDataReader dataReader)
	{
		return Text.Read(dataReader, textFragments);
    }

	public IText LoadText(AssetIdentifier assetIdentifier)
	{
        bool singleString = true;
		bool containerAsset = false;
		bool usingFragments = true;

		switch (assetIdentifier.Type)
		{
			case AssetType.SpellName:
			case AssetType.SpellSchoolName:
			case AssetType.ClassName:
            case AssetType.RaceName:
            case AssetType.AttributeName:
            case AssetType.SkillName:
			case AssetType.CharInfoText:
			case AssetType.LanguageName:
			case AssetType.ConditionName:
			case AssetType.ItemTypeName:
                singleString = true;
                break;
			case AssetType.MapText:
			case AssetType.ItemText:
			case AssetType.PuzzleText:
                singleString = false;
				break;
            case AssetType.Message:
                containerAsset = true;                
				break;
            case AssetType.UIText:
                usingFragments = false;
				break;
            default:
				throw new AmberException(ExceptionScope.Application, $"Invalid text asset type {assetIdentifier.Type}.");
		}

		if (containerAsset)
		{
			int textBlockIndex = assetIdentifier.Index;

			for (int containerIndex = 1; containerIndex < 100; containerIndex++)
			{
				var containerAssetIdentifier = new AssetIdentifier(assetIdentifier.Type, containerIndex);

				if (!texts.TryGetValue(containerAssetIdentifier, out var container))
				{
					var asset = assetProvider.GetAsset(containerAssetIdentifier);

					if (asset == null)
						throw new AmberException(ExceptionScope.Data, $"Asset {assetIdentifier} not found.");

					container = Text.Load(asset, textFragments);

					texts.Add(containerAssetIdentifier, container);
				}

				if (textBlockIndex < container.TextBlockCount)
				{
					return container.GetTextBlock(textBlockIndex);
				}

				textBlockIndex -= container.TextBlockCount;
        }
		}

        if (!texts.TryGetValue(assetIdentifier, out var text))
		{
            var asset = assetProvider.GetAsset(assetIdentifier);

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Asset {assetIdentifier} not found.");

			if (usingFragments)
			{
				text = singleString ? Text.LoadSingleString(asset, textFragments) : Text.Load(asset, textFragments);
			}
			else
			{
				var reader = asset.GetReader();
				text = Text.FromString(reader.ReadString(reader.Size));
			}

            texts.Add(assetIdentifier, text);
		}

		return text;
	}
}
