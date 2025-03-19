namespace Amberstar.Game;

public enum GameOption
{
    /// <summary>
    /// In the original when dragging items, they are masked (only white color, as it is a cursor).
    /// We support dragging the item directly without masking it.
    /// </summary>
    UnmaskedDraggedItem
}

partial class Game
{
    // TODO
    public bool IsOptionSet(GameOption option)
    {
        if (option == GameOption.UnmaskedDraggedItem)
            return true;

        return false;
    }
}
