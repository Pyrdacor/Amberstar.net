using Amber.Assets.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Serialization;

public interface IItemLoader
{
	IItem LoadItem(IAsset asset);
    IItem ReadItem(IDataReader reader);
}
