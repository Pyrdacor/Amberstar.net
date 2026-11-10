using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;
using Amberstar.GameData;

namespace Amberstar.Tests;

public sealed class CheatTests
{
	static HeadlessGame StartGame()
	{
		var game = new HeadlessGame(TestData.GetDataPath(GameSetup.RepositoryDataFolders[0]));
		var result = game.RunScript(new ScenarioScript(["waittext", "dismissall"], _ => { }), output: _ => { });

		Assert.True(result.Success, game.Game.ScriptState());

		return game;
	}

	static (int MapIndex, int MapCharacterIndex) FindPerson(Game.Game game, int characterIndex)
	{
		foreach (var (mapIndex, map) in game.AssetProvider.MapLoader.LoadAllMaps().OrderBy(map => map.Key))
		{
			int slot = Array.FindIndex(map.Characters, character => character.Type == MapCharacterType.Person && character.Index == characterIndex);

			if (slot >= 0)
				return (mapIndex, slot + 1);
		}

		throw new InvalidOperationException($"Character {characterIndex} is on no map.");
	}

	[Fact]
	public void InviteAddsThePartyMemberAndRemovesThePersonFromItsMap()
	{
		using var game = StartGame();
		var (mapIndex, mapCharacterIndex) = FindPerson(game.Game, 62);

		Assert.True(game.Game.State.IsMapCharacterActive(mapIndex, mapCharacterIndex));

		Cheats.Execute(game.Game, "invite 62");

		Assert.Equal(2, game.Game.ScriptPartySize);
		Assert.Contains(62, game.Game.State.PartyCharacterIndices);
		Assert.False(game.Game.State.IsMapCharacterActive(mapIndex, mapCharacterIndex));

		// a second invitation does nothing
		Cheats.Execute(game.Game, "invite 62");
		Assert.Equal(2, game.Game.ScriptPartySize);

		// after the dismissal the person is on its map again
		game.Game.State.RemovePartyMember(2);
		Assert.True(game.Game.State.IsMapCharacterActive(mapIndex, mapCharacterIndex));
	}

	[Fact]
	public void InviteFindsThePartyMemberByName()
	{
		using var game = StartGame();
		var (characterIndex, name, _) = game.Game.GetPossiblePartyMembers().First(member => member.CharacterIndex == 62);

		Cheats.Execute(game.Game, $"invite {name}");

		Assert.Contains(characterIndex, game.Game.State.PartyCharacterIndices);
	}

	[Fact]
	public void InviteIgnoresTheJoinChance()
	{
		using var game = StartGame();
		var personLoader = game.Game.AssetProvider.PersonLoader;
		// the character who joins least likely in a conversation
		var (characterIndex, joinChance) = game.Game.GetPossiblePartyMembers().Where(member => !member.InParty)
			.Select(member => (member.CharacterIndex, (int)personLoader.LoadPerson(member.CharacterIndex).ConversationData.JoinChance))
			.MinBy(member => member.Item2);

		Assert.True(joinChance < 100);
		Assert.Null(game.Game.InvitePartyMember(characterIndex));
		Assert.Contains(characterIndex, game.Game.State.PartyCharacterIndices);
	}

	[Fact]
	public void OnlyPossiblePartyMembersCanBeInvited()
	{
		using var game = StartGame();
		var possible = game.Game.GetPossiblePartyMembers().Select(member => member.CharacterIndex).ToHashSet();
		int other = Enumerable.Range(1, 200).First(index => !possible.Contains(index));

		Assert.NotNull(game.Game.InvitePartyMember(other));
		Assert.Equal(1, game.Game.ScriptPartySize);
	}
}
