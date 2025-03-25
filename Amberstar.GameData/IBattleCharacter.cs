using System.Collections.ObjectModel;

namespace Amberstar.GameData;

public interface IBattleCharacter : ICharacter
{
    byte UsedHands { get; set; }
    byte UsedFingers { get; set; }
    byte Defense { get; set; }
    byte Damage { get; set; }
    PhysicalCondition PhysicalConditions { get; set; }
    MentalCondition MentalConditions { get; set; }
    public byte MagicBonusWeapon { get; set; }
    public byte MagicBonusArmor { get; set; }
    public byte AttacksPerRound { get; set; }
    CharacterValue HitPoints { get; }
    CharacterValue SpellPoints { get; }
    ReadOnlyDictionary<Skill, CharacterValue> Skills { get; }
    ReadOnlyDictionary<Attribute, CharacterValue> Attributes { get; }
}

public static class BattleCharacterExtensions
{
    public static bool IsDead(this IBattleCharacter character) =>
        character.PhysicalConditions.HasFlag(PhysicalCondition.Dead) ||
        character.PhysicalConditions.HasFlag(PhysicalCondition.Ashes) ||
        character.PhysicalConditions.HasFlag(PhysicalCondition.Dust);
}