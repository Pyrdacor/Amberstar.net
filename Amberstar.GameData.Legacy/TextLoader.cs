using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class TextLoader(Amber.Assets.Common.IAssetProvider assetProvider, List<string> textFragments) : ITextLoader
{
	readonly List<string> textFragments = textFragments;
	readonly Dictionary<AssetIdentifier, IText> texts = [];

    public IText FromString(string text)
	{
        return Text.FromString(text);
    }

    public IText ReadText(IDataReader dataReader)
	{
		return Text.Read(dataReader, textFragments);
    }

	public IText LoadText(AssetIdentifier assetIdentifier)
	{
		bool singleString = true;
		bool usingFragments = true;

		switch (assetIdentifier.Type)
		{
			case AssetType.SpellName:
			case AssetType.SpellSchoolName:
			case AssetType.ClassName:
			case AssetType.SkillName:
			case AssetType.CharInfoText:
			case AssetType.RaceName:
			case AssetType.ConditionName:
			case AssetType.ItemTypeName:
				singleString = true;
                break;
			case AssetType.MapText:
			case AssetType.ItemText:
			case AssetType.PuzzleText:
				singleString = false;
				break;
            case AssetType.UIText:
                usingFragments = false;
				break;
            default:
				throw new AmberException(ExceptionScope.Application, $"Invalid text asset type {assetIdentifier.Type}.");
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
