namespace Amberstar.GameData;

public enum Language : byte
{
	Human,
	Elf,
	Dwarf,
	Gnome,
	Halfling,
    Orc,
    Animal
}

[Flags]
public enum LanguageFlags : byte
{
    None = 0,
    Human = 1 << Language.Human,
    Elf = 1 << Language.Elf,
    Dwarf = 1 << Language.Dwarf,
    Gnome = 1 << Language.Gnome,
    Halfling = 1 << Language.Halfling,
    Orc = 1 << Language.Orc,
    Animal = 1 << Language.Animal,
}