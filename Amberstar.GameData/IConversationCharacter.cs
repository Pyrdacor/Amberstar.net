using Amber.Assets.Common;

namespace Amberstar.GameData;

public interface IConversationCharacter : ICharacter
{
    LanguageFlags LearnedLanguages { get; set; }
    byte JoinChance { get; init; }
    byte QuestCompletionIndex { get; init; }
    IGraphic Portrait { get; init; }
    IText[] Texts { get; init; }
    Dictionary<InteractionTrigger, IConversationInteraction> PrimaryInteractions { get; }
    Dictionary<InteractionTrigger, IConversationInteraction> SecondaryInteractions { get; }
}    
