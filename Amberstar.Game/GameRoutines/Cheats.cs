using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// Functionality for the cheats of the frontend.
/// </summary>
partial class Game
{
	/// <summary>
	/// The party can move through walls and other obstacles on 2D and 3D maps (like the super chicken of the original).
	/// </summary>
	public bool NoClip { get; set; } = false;

	/// <summary>
	/// All characters who can join the party with their character index (index in CHARDATA.AMB).
	/// </summary>
	public IEnumerable<(int CharacterIndex, string Name, bool InParty)> GetPossiblePartyMembers() =>
		State.PossiblePartyMembers.Select(member => (member.CharacterIndex, member.PartyMember.Name, State.PartyCharacterIndices.Contains(member.CharacterIndex)))
			.OrderBy(member => member.CharacterIndex);

	/// <summary>
	/// Adds a party member like asking to join in a conversation: the person is removed from its map and shown
	/// there again after it was dismissed. Unlike the conversation the join chance of the person doesn't matter,
	/// every possible party member joins. Returns an error message or null.
	/// </summary>
	public string? InvitePartyMember(int characterIndex)
	{
		if (!State.IsPossiblePartyMember(characterIndex))
			return $"Character {characterIndex} can't join the party.";

		if (State.PartyCharacterIndices.Contains(characterIndex))
			return $"Character {characterIndex} is already in the party.";

		if (!State.TryAddPartyMember(characterIndex, out int slotIndex))
			return "The party is full.";

		// the first map where the person is, like Joined in the conversation
		foreach (var (mapIndex, map) in AssetProvider.MapLoader.LoadAllMaps().OrderBy(map => map.Key))
		{
			int slot = Array.FindIndex(map.Characters, character => character.Type == MapCharacterType.Person && character.Index == characterIndex);

			if (slot < 0)
				continue;

			int mapCharacterIndex = slot + 1; // 1-based for the save bits
			State.GetPartyMember(slotIndex + 1)!.SaveBit = (word)((mapIndex - 1) * IMap.CharacterCount + mapCharacterIndex);
			State.SetMapCharacterActive(mapIndex, mapCharacterIndex, false);
			break;
		}

		UpdatePartyMembers();

		return null;
	}
}
