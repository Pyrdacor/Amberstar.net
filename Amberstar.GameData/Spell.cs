namespace Amberstar.GameData;

public enum WhiteSpell : byte
{
	None,
	Healing1, // 1
	Healing2,
	Healing3,
	Healing4,
	Healing5, // 5
	Salvation,
    Reincarnation,
	ConversionOfAshes,
	ConversionOfDust,
	NeutralizePoison, // 10
	HealStun,
	HealSickness,
	Rejuvation,
	Depetrification,
	WakeUp, // 15
	CalmPanic,
	RemoveIrritation,
	HealBlindness,
	HealMadness,
	Stun, // 20
	Sleep,
	Fear,
	Irritation,
	Blind,
	DestroyUndead, // 25
	HolyWord,
	RemoveCurse,
	ProvideFood,
	Unused29,
	Unused30, // 30
}

public enum GraySpell : byte
{
    None,
    Light1, // 1
    Light2,
    Light3,
    ArmorProtection1,
    ArmorProtection2, // 5
    ArmorProtection3,
    WeaponsPower1,
    WeaponsPower2,
    WeaponsPower3,
    AntiMagic1, // 10
    AntiMagic2,
    AntiMagic3,
    Clairvoyance1,
    Clairvoyance2,
    Clairvoyance3, // 15
    Invisibility1,
    Invisibility2,
    Invisibility3,
    MagicSphere,
    MagicCompass, // 20
    Identification,
    Levitation,
    Haste,
    MassHaste,
    Teleport, // 25
    XRayVision,
    Unused27,
    Unused28,
    Unused29,
    Unused30, // 30
}

public enum BlackSpell : byte
{
    None,
    BeamOfFire, // 1
    WallOfFire,
    Fireball,
    Firestorm,
    FireCascade, // 5
    Waterhole,
    Waterfall,
    Iceball,
    IceShower,
    HailStorm, // 10
    MudCatapult,
    FallingRock,
    Bog,
    Landslide,
    Earthquake, // 15
    StrongWind,
    Storm,
    Tornado,
    Thunder,
    Hurricane, // 20
    Desintegration,
    MagicArrows,
    Unused23,
    Unused24,
    Unused25, // 25
    Unused26,
    Unused27,
    Unused28,
    Unused29,
    Unused30, // 30
}

public enum SpecialSpell : byte
{
    None,
    Stun, // 1
    Poison,
    FleshToStone,
    MakeIll,
    Aging, // 5
    Irritation,
    MakeMad,
    Sleep,
    Panic,
    BlindingFlash, // 10
    FleshToStone2, // TODO?
    Mapshow,
    BanishDemon,
    SpellPoints1,
    SpellPoints2, // 15
    WeaponBalm,
    Youth,
    PickLock,
    EagleCall,
    Music, // 20
    Unused21,
    Unused22,
    Unused23,
    Unused24,
    Unused25, // 25
    Unused26,
    Unused27,
    Unused28,
    Unused29,
    Unused30, // 30
}