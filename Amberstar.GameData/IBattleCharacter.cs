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
    byte BattleGraphicIndex { get; init; }
    byte MagicArmorLevel { get; set; }
    byte AttacksPerRound { get; set; }
    CharacterValue HitPoints { get; }
    CharacterValue SpellPoints { get; }
    ReadOnlyDictionary<Skill, CharacterValue> Skills { get; }
    ReadOnlyDictionary<Attribute, CharacterValue> Attributes { get; }
}    
