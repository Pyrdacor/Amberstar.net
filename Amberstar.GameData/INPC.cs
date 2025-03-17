using Amber.Assets.Common;

namespace Amberstar.GameData;

public interface INPC : ICharacter
{
    IConversationData ConversationData { get; init; }
    public CharacterValue Age => ConversationData.Age;
    public IGraphic? Portrait => ConversationData.Portrait;
}
