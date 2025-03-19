using Amber.Assets.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class ItemLoader : IItemLoader
	{
		public IItem LoadItem(IAsset asset)
        {
            var reader = asset.GetReader();

            return ReadItem( reader);
		}

        public IItem ReadItem(IDataReader reader)
        {
            int offset = reader.Position;

            var graphicIndex = (ItemGraphic)reader.ReadByte();

            reader.Position = 0x24;
            var index = reader.ReadWord();

            reader.Position = offset + 40; // TODO: we need it until the method is fully implemented

            // TODO ...
            return new Item()
            {
                Index = index,
                GraphicIndex = graphicIndex
            };
        }
    }
}