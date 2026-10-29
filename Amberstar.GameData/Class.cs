namespace Amberstar.GameData;

public enum Class : byte
{
	None,
	Warrior,
	Paladin,
	Ranger,
	Thief,
	Monk,
	WhiteWizzard,
	GrayWizzard,
	BlackWizzard,
	Animal,
	Monster
}

[Flags]
public enum ClassFlags : word
{
    None = 0,
    Classless = 1 << Class.None,
    Warrior = 1 << Class.Warrior,
    Paladin = 1 << Class.Paladin,
    Ranger = 1 << Class.Ranger,
    Thief = 1 << Class.Thief,
    Monk = 1 << Class.Monk,
    WhiteWizzard = 1 << Class.WhiteWizzard,
    GrayWizzard = 1 << Class.GrayWizzard,
    BlackWizzard = 1 << Class.BlackWizzard,
    Animal = 1 << Class.Animal,
    Monster = 1 << Class.Monster
}