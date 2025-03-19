using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class PartyMemberLoader(Amber.Assets.Common.IAssetProvider assetProvider, Lazy<ITextLoader> textLoader) : IPartyMemberLoader
    {
		readonly Dictionary<int, IPartyMember> partyMembers = [];

		public IPartyMember LoadPartyMember(int index)
		{
			if (!partyMembers.TryGetValue(index, out var partyMember))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Player, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Party member {index} not found.");

				partyMember = PartyMember.Load(asset, textLoader.Value);
				partyMembers.Add(index, partyMember);
			}

			return partyMember;
		}
	}
}
