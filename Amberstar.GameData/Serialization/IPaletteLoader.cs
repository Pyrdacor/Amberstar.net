using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization;

public enum BuiltinPalette
{
    UI,
    Item,
	Automap,
	Puzzle, // the altar where the Amberstar is assembled
	Cursor, // the mouse pointer (only the DOS version has its own colors, otherwise it is the UI palette)
}

public interface IPaletteLoader
{
	IGraphic LoadPalette(int index);
	IGraphic LoadBuiltinPalette(BuiltinPalette builtinPalette);
}
