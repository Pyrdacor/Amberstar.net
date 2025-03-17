using Amber.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Serialization;

public interface ITextLoader
{
	IText LoadText(AssetIdentifier assetIdentifier);
    IText ReadText(IDataReader dataReader);
}
