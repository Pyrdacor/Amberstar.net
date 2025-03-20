using Amberstar.GameData.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar.Game;

public interface IPaletteIndexProvider
{
	ReadOnlyDictionary<BuiltinPalette, byte> BuiltinPaletteIndices { get; }

    byte GetTilesetPaletteIndex(int tileset);
	byte GetLabyrinthPaletteIndex(int paletteIndex); // not labyrinth index!
	byte Get80x80ImagePaletteIndex(Image80x80 image80X80);
	byte GetTextPaletteIndex();
}
