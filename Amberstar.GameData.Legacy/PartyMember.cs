using Amber.Assets.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class PartyMember : BattleCharacter, IPartyMember
{
    private ClassFlags possibleClasses;
    private byte defaultBattlePosition;
    private word attackPerRoundLevel;
    private word hitPointsPerLevel;
    private word spellPointsPerLevel;
    private word spellLearningPointsPerLevel;
    private word spellLearningPoints;
    private IConversationData? conversationData;

    public static PartyMember Load(IAsset asset, ITextLoader textLoader)
    {
        var partyMember = new PartyMember();
        var reader = asset.GetReader();

        BattleCharacter.Load(partyMember, reader);
        partyMember.conversationData = Legacy.ConversationData.Load(reader, textLoader);

        reader.Position = 0x1a;
        partyMember.LearnedSpellSchools = (SpellSchoolFlags)reader.ReadByte();

        reader.Position = 0x42;
        partyMember.defaultBattlePosition = reader.ReadByte();

        reader.Position = 0x46;
        partyMember.possibleClasses = (ClassFlags)reader.ReadWord();

        reader.Position = 0x70;
        partyMember.attackPerRoundLevel = reader.ReadWord();
        partyMember.hitPointsPerLevel = reader.ReadWord();
        partyMember.spellPointsPerLevel = reader.ReadWord();
        partyMember.spellLearningPointsPerLevel = reader.ReadWord();

        reader.Position = 0x8e;
        partyMember.spellLearningPoints = reader.ReadWord();

        reader.Position = 0xcc;
        partyMember.ExperiencePoints = reader.ReadDword();
        partyMember.LearnedWhiteSpells = reader.ReadDword();
        partyMember.LearnedGraySpells = reader.ReadDword();
        partyMember.LearnedBlackSpells = reader.ReadDword();
        reader.Position += 3 * 4; // Skip unused spells
        partyMember.LearnedSpecialSpells = reader.ReadDword();

        // We ensure that some equipment related values are correctly set.


        return partyMember;
    }

    public ClassFlags PossibleClasses { get => possibleClasses; init => possibleClasses = value; }
    public byte DefaultBattlePosition { get => defaultBattlePosition; init => defaultBattlePosition = value; }
    public word AttackPerRoundLevel { get => attackPerRoundLevel; init => attackPerRoundLevel = value; }
    public word HitPointsPerLevel { get => hitPointsPerLevel; init => hitPointsPerLevel = value; }
    public word SpellPointsPerLevel { get => spellPointsPerLevel; init => spellPointsPerLevel = value; }
    public word SpellLearningPointsPerLevel { get => spellLearningPointsPerLevel; init => spellLearningPointsPerLevel = value; }
    public word SpellLearningPoints { get => spellLearningPoints; init => spellLearningPoints = value; }
    public dword ExperiencePoints { get; set; }
    public SpellSchoolFlags LearnedSpellSchools { get; set; }
    public dword LearnedWhiteSpells { get; set; }
    public dword LearnedGraySpells { get; set; }
    public dword LearnedBlackSpells { get; set; }
    public dword LearnedSpecialSpells { get; set; }
    public dword TotalWeight { get; }
    public IConversationData ConversationData { get => conversationData ?? throw new NullReferenceException("conversationData is null"); init => conversationData = value; }
}
