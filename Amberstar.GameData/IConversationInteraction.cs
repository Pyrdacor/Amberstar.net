namespace Amberstar.GameData;

public enum InteractionTriggerType : byte
{
    Say = 1, // keyword
    Show, // item
    Give, // item
    Pay, // gold
    Feed, // food
    Join, // party
}

public record InteractionTrigger(InteractionTriggerType Type, word Argument = 0);

public interface IConversationInteraction
{

}

/// <summary>
/// The NPC responds with a text message.
/// </summary>
public interface ISayReaction : IConversationInteraction
{
    int MessageIndex { get; init; }
}

/// <summary>
/// The NPC teaches you a new word which can be used in future
/// conversations.
/// </summary>
public interface ITeachWordReaction : IConversationInteraction
{
    int WordIndex { get; init; }
}

/// <summary>
/// The NPC gives you an item.
/// 
/// The source item must be in the NPC's inventory or equipped by it.
/// A copy of the item is used so the source item remains.
/// </summary>
public interface IGiveItemReaction : IConversationInteraction
{
    /// <summary>
    /// This is the slot index of the item in the character's item data.
    /// So index 0 is the first equipped item and index 9 is the first
    /// inventory item slot.
    /// </summary>
    int ItemSlotIndex { get; init; }
}

/// <summary>
/// The NPC gives you some gold.
/// </summary>
public interface IGiveGoldReaction : IConversationInteraction
{
    word Amount { get; init; }
}

/// <summary>
/// The NPC gives you some food.
/// </summary>
public interface IGiveFoodReaction : IConversationInteraction
{
    word Amount { get; init; }
}

/// <summary>
/// The NPC completes the given quest.
/// 
/// A quest index is usually stored inside NPCs and unlocks a second
/// pair of interactions for the NPC.
/// </summary>
public interface ICompleteQuestReaction : IConversationInteraction
{
    byte QuestIndex { get; init; }
}

public enum ChangeStatAction
{
    Increase = 1,
    SetBit = 7,
}

/// <summary>
/// The NPC changes some stat of the player.
/// 
/// A quest index is usually stored inside NPCs and unlocks a second
/// pair of interactions for the NPC.
/// </summary>
public interface IChangeStatReaction : IConversationInteraction
{
    /// <summary>
    /// Offset into the character data.
    /// </summary>
    byte StatOffset { get; init; }

    ChangeStatAction Action { get; init; }

    /// <summary>
    /// Amount, bit index, etc.
    /// </summary>
    word Value { get; init; }
}