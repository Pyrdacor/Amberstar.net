using Amber.Assets.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Person : Character, IPerson
{
    private IConversationData? conversationData;

    public static IPerson Load(IAsset asset, ITextLoader textLoader)
    {
        var reader = asset.GetReader();

        reader.Position = 0x3c; // Offset to join percentage
        byte joinChance = reader.ReadByte();
        reader.Position = 0;

        if (joinChance != 0) // Can join? Then it is a party member.
            return PartyMember.Load(asset, textLoader);

        var person = new Person();        

        Character.Load(person, reader);
        person.conversationData = Legacy.ConversationData.Load(reader, textLoader);

        return person;
    }

    public IConversationData ConversationData { get => conversationData ?? throw new NullReferenceException("conversationData is null"); init => conversationData = value; }
}
