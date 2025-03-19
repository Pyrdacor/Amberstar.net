using Amberstar.GameData.Serialization;

namespace Amberstar.GameData;

public interface IItem
{
    uint Index { get; }
    ItemGraphic GraphicIndex { get; }

    // TODO ...
}

public static class ItemExtensions
{
    public static bool IsTwoHanded(this IItem item)
    {
        // TODO ...
        return false;
    }

    public static bool CanThrowAway(this IItem item)
    {
        // TODO ...
        return false;
    }

    public static bool IsStackable(this IItem item)
    {
        // TODO ...
        return false;
    }
}