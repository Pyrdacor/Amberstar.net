namespace Amberstar.GameData;

public interface IPartyMember : IBattleCharacter, IConversationCharacter
{
    ClassFlags PossibleClasses { get; init; }
    byte DefaultBattlePosition { get; init; }
    byte AttackPerRoundLevel { get; init; }
    word HitPointsPerLevel { get; init; }
    word SpellPointsPerLevel { get; init; }
    word SpellLearningPointsPerLevel { get; init; }
    word TrainingPointsPerLevel { get; init; }
    dword ExperiencePoints { get; set; }
    SpellSchoolFlags LearnedSpellSchools { get; set; }
    dword LearnedWhiteSpells { get; set; }
    dword LearnedGraySpells { get; set; }
    dword LearnedBlackSpells { get; set; }
    dword LearnedSpecialSpells { get; init; }
    dword TotalWeight { get; } // We should calculate it and only use it for display etc

    /*
     * Level Up:
     * 
     * - Increase level by 1
     * - If APRPerLvl == 0, don't change APR
     * - Otherwise APR = level / APRPerLvl
     * - HP += (TotalSTA / 10) + HPPerLvl
     * - If magic:
     * -  SP += (TotalINT / 20) + SPPerLvl
     * -  SLP += (TotalINT / 20) + SLPPerLvl
     */
}
