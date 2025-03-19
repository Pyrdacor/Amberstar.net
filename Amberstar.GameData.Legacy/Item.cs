using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Item : IItem
{
    public uint Index { get; init; }
    public ItemGraphic GraphicIndex { get; init; }
}
