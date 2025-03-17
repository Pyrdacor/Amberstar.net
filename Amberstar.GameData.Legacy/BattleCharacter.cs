using Amber.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar.GameData.Legacy;

internal class BattleCharacter : Character, IBattleCharacter
{
    private readonly Dictionary<Skill, CharacterValue> skills = [];
    private readonly Dictionary<Attribute, CharacterValue> attributes = [];

    public static void Load(BattleCharacter character, IDataReader reader)
    {
        Character.Load(character, reader);

        // Skills
        reader.Position = 0x06;
        var currentAttack = reader.ReadByte();
        var currentParry = reader.ReadByte();
        var currentSwim = reader.ReadByte();
        var currentListen = reader.ReadByte();
        var currentFindTraps = reader.ReadByte();
        var currentDisarmTraps = reader.ReadByte();
        var currentPickLocks = reader.ReadByte();
        var currentSearch = reader.ReadByte();
        var currentReadMagic = reader.ReadByte();
        var currentUseMagic = reader.ReadByte();
        var maxAttack = reader.ReadByte();
        var maxParry = reader.ReadByte();
        var maxSwim = reader.ReadByte();
        var maxListen = reader.ReadByte();
        var maxFindTraps = reader.ReadByte();
        var maxDisarmTraps = reader.ReadByte();
        var maxPickLocks = reader.ReadByte();
        var maxSearch = reader.ReadByte();
        var maxReadMagic = reader.ReadByte();
        var maxUseMagic = reader.ReadByte();

        character.skills[Skill.Attack] = new(currentAttack, maxAttack);
        character.skills[Skill.Parry] = new(currentParry, maxParry);
        character.skills[Skill.Swim] = new(currentSwim, maxSwim);
        character.skills[Skill.Listen] = new(currentListen, maxListen);
        character.skills[Skill.FindTraps] = new(currentFindTraps, maxFindTraps);
        character.skills[Skill.DisarmTraps] = new(currentDisarmTraps, maxDisarmTraps);
        character.skills[Skill.PickLocks] = new(currentPickLocks, maxPickLocks);
        character.skills[Skill.Search] = new(currentSearch, maxSearch);
        character.skills[Skill.ReadMagic] = new(currentReadMagic, maxReadMagic);
        character.skills[Skill.UseMagic] = new(currentUseMagic, maxUseMagic);

        // Used hand & fingers, def & dmg
        reader.Position = 0x1c;
        character.UsedHands = reader.ReadByte();
        character.UsedFingers = reader.ReadByte();
        character.Defense = reader.ReadByte();
        character.Damage = reader.ReadByte();
        character.MagicBonusWeapon = reader.ReadByte();
        character.MagicBonusArmor = reader.ReadByte();

        // Physical & mental conditions
        reader.Position = 0x3a;
        character.PhysicalConditions = (PhysicalCondition)reader.ReadByte();
        character.MentalConditions = (MentalCondition)reader.ReadByte();

        // Battle graphic index
        reader.Position = 0x43;
        character.AttacksPerRound = reader.ReadByte();

        // Attributes
        reader.Position = 0x48;
        var currentStrength = reader.ReadWord();
        var currentIntelligence = reader.ReadWord();
        var currentDexterity = reader.ReadWord();
        var currentSpeed = reader.ReadWord();
        var currentStamina = reader.ReadWord();
        var currentCharisma = reader.ReadWord();
        var currentLuck = reader.ReadWord();
        var currentAntiMagic = reader.ReadWord();
        var currentAge = reader.ReadWord();
        var currentUnusedAttribute = reader.ReadWord();
        var maxStrength = reader.ReadWord();
        var maxIntelligence = reader.ReadWord();
        var maxDexterity = reader.ReadWord();
        var maxSpeed = reader.ReadWord();
        var maxStamina = reader.ReadWord();
        var maxCharisma = reader.ReadWord();
        var maxLuck = reader.ReadWord();
        var maxAntiMagic = reader.ReadWord();
        var maxAge = reader.ReadWord();
        var maxUnusedAttribute = reader.ReadWord();

        character.attributes[Attribute.Strength] = new(currentStrength, maxStrength);
        character.attributes[Attribute.Intelligence] = new(currentIntelligence, maxIntelligence);
        character.attributes[Attribute.Dexterity] = new(currentDexterity, maxDexterity);
        character.attributes[Attribute.Speed] = new(currentSpeed, maxSpeed);
        character.attributes[Attribute.Stamina] = new(currentStamina, maxStamina);
        character.attributes[Attribute.Charisma] = new(currentCharisma, maxCharisma);
        character.attributes[Attribute.Luck] = new(currentLuck, maxLuck);
        character.attributes[Attribute.AntiMagic] = new(currentAntiMagic, maxAntiMagic);
        character.attributes[Attribute.Age] = new(currentAge, maxAge);
        character.attributes[Attribute.Unused] = new(currentUnusedAttribute, maxUnusedAttribute);

        reader.Position = 0x86;
        var currentHitPoints = reader.ReadWord();
        var maxHitPoints = reader.ReadWord();
        var currentSpellPoints = reader.ReadWord();
        var maxSpellPoints = reader.ReadWord();
        character.HitPoints = new(currentHitPoints, maxHitPoints);
        character.SpellPoints = new(currentSpellPoints, maxSpellPoints);
    }

    public byte UsedHands { get; set; }
    public byte UsedFingers { get; set; }
    public byte Defense { get; set; }
    public byte Damage { get; set; }
    public PhysicalCondition PhysicalConditions { get; set; }
    public MentalCondition MentalConditions { get; set; }
    public byte MagicBonusWeapon { get; set; }
    public byte MagicBonusArmor { get; set; }
    public byte AttacksPerRound { get; set; }
    public CharacterValue HitPoints { get; private set; } = new(0, 0);
    public CharacterValue SpellPoints { get; private set; } = new(0, 0);
    public ReadOnlyDictionary<Skill, CharacterValue> Skills => skills.AsReadOnly();
    public ReadOnlyDictionary<Attribute, CharacterValue> Attributes => attributes.AsReadOnly();
}
