using Amber.Serialization;

namespace Amberstar.GameData.Serialization;

public interface IItemLoader
{
	IItem LoadItem(IDataReader reader);
}
