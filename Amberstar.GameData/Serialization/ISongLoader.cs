using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization;

public interface ISongLoader
{
	ISong LoadSong(int musicIndex, int songIndex);
}
