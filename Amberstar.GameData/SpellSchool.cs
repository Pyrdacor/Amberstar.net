namespace Amberstar.GameData;

public enum SpellSchool : byte
{
	None,
	White,
	Gray,
	Black,
	Unused4,
	Unused5,
	Unused6,
	Special,
}

[Flags]
public enum SpellSchoolFlags : byte
{
    None = 0,
    White = 1 << SpellSchool.White,
    Gray = 1 << SpellSchool.Gray,
    Black = 1 << SpellSchool.Black,
    Special = 1 << SpellSchool.Special,
}

public static class SpellSchoolExtensions
{
	public static bool CanBeLearned(this SpellSchool school) => school switch
	{
		SpellSchool.White => true,
		SpellSchool.Gray => true,
		SpellSchool.Black => true,
		_ => false
	};

	public static bool IsUsed(this SpellSchool school) => school switch
	{
		SpellSchool.None => true,
		SpellSchool.White => true,
		SpellSchool.Gray => true,
		SpellSchool.Black => true,
		SpellSchool.Special => true,
		_ => false
	};
}