using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class PersonLoader(Amber.Assets.Common.IAssetProvider assetProvider, Lazy<ITextLoader> textLoader) : IPersonLoader
    {
		readonly Dictionary<int, IPerson> persons = [];

		public IPerson LoadPerson(int index)
		{
			if (!persons.TryGetValue(index, out var person))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Person, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Person {index} not found.");

				person = Person.Load(asset, textLoader.Value);
				persons.Add(index, person);
			}

			return person;
		}
	}
}
