using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization;

public enum BuiltinPalette
{
    UI,
    Item,
	Automap,
}

public interface IPaletteLoader
{
	IGraphic LoadPalette(int index);
	IGraphic LoadBuiltinPalette(BuiltinPalette builtinPalette);
}
