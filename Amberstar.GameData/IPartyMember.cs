namespace Amberstar.GameData;

public interface IPartyMember : IBattleCharacter, IConversationCharacter
{
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
}    
