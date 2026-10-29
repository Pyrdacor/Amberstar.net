namespace Amberstar.GameData;

public enum Gender : byte
{
	Male,
	Female
}

[Flags]
public enum GenderFlags : byte
{
    Both = 0x0,
    Male = 0x1,
    Female = 0x2
}