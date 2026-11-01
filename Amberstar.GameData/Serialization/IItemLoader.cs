using Amber.Assets.Common;
using Amber.IO.Common.Serialization;

namespace Amberstar.GameData.Serialization;

public interface IItemLoader
{
	IStaticItemData LoadItem(uint index);
	IEnumerable<IStaticItemData> LoadAllItems();
    IItem ReadItem(IDataReader reader);
}
