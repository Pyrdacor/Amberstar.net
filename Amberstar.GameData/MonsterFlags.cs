namespace Amberstar.GameData;

[Flags]
public enum MonsterFlags : byte
{
	Undead = 0x01,
	Demon = 0x02,
	Boss = 0x04,
}

[Flags]
public enum MonsterElementalFlags : byte
{
    FireImmune = 0x01,
    EarthImmune = 0x02,
    WaterImmune = 0x04,
    WindImmune = 0x08,
    FireVulnerable = 0x10,
    EarthVulnerable = 0x20,
    WaterVulnerable = 0x40,
    WindVulnerable = 0x80,
}