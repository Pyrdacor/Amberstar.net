namespace Amberstar.GameData.Serialization;

public interface ICursorLoader
{
	ICursor LoadCursor(CursorType cursorType);

	/// <summary>
	/// The cursors have their own colors and always use the cursor palette (DOS version).
	/// Otherwise they use the palette of the map like the original programs.
	/// </summary>
	bool OwnPalette { get; }
}
