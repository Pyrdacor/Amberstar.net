using Amber.Assets.Common;

namespace Amberstar.GameData;

public interface IPerson : ICharacter
{
    IConversationData ConversationData { get; init; }
    public CharacterValue Age => ConversationData.Age;
    public IGraphic? Portrait => ConversationData.Portrait;
}
