using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class MonsterLoader(Amber.Assets.Common.IAssetProvider assetProvider) : IMonsterLoader
	{
		readonly Dictionary<int, IMonster> monsters = [];

		public IMonster LoadMonster(int index)
		{
			if (!monsters.TryGetValue(index, out var monster))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Monster, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Monster {index} not found.");

				monster = Monster.Load(asset);
				monsters.Add(index, monster);
			}

			return monster;
		}
	}
}
