using Amber.Common;
using Amberstar.Game;
using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;
using Amberstar.GameData;
using G = Amberstar.Game.Game;

namespace Amberstar.Tests;

/// <summary>
/// Runs the game headless and checks basic game logic.
/// </summary>
public sealed class HeadlessGameTests
{
	static HeadlessGame CreateGame() => new(TestData.GetDataPath(GameSetup.RepositoryDataFolders[0]));

	/// <summary>
	/// Starts the game and closes the intro text of the graveyard.
	/// </summary>
	static HeadlessGame StartGame()
	{
		var game = CreateGame();
		var result = game.RunScript(new ScenarioScript(["waittext", "dismissall"], _ => { }), output: _ => { });

		Assert.True(result.Success, game.Game.ScriptState());

		return game;
	}

	[Fact]
	public void GameStartsWithTheGraveyardText()
	{
		using var game = CreateGame();

		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "PictureText"), game.Game.ScriptState());
		Assert.Equal(65, game.Game.ScriptMapIndex);
		Assert.Single(game.Game.ScriptShownTexts);
	}

	[Fact]
	public void IntroShowsTheCreditsAndStartsTheGame()
	{
		using var game = new HeadlessGame(TestData.GetDataPath(GameSetup.RepositoryDataFolders[0]), showIntro: true);

		// the logo of Thalion ends with a key
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "ThalionLogo"), game.Game.ScriptState());
		game.Step(60);
		game.Game.KeyDown(Amberstar.Game.Key.Space, Amberstar.Game.KeyModifiers.None);
		game.Game.KeyUp(Amberstar.Game.Key.Space, Amberstar.Game.KeyModifiers.None);
		// the title picture stays until the space key is pressed
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "Intro", 300), game.Game.ScriptState());
		game.Step(120);
		Assert.Empty(game.Game.ScriptShownTexts);
		game.Game.KeyDown(Amberstar.Game.Key.Space, Amberstar.Game.KeyModifiers.None);
		// the credits after the fading and the logo
		Assert.True(game.StepUntil(() => game.Game.ScriptShownTexts.Count == 1, 300), game.Game.ScriptState());
		Assert.Contains("Karsten", game.Game.ScriptShownTexts[0]);
		Assert.True(game.StepUntil(() => game.Game.ScriptShownTexts.Count == 2, 400), game.Game.ScriptState());
		// the space key ends the intro and the game starts with the graveyard text
		game.Game.KeyDown(Amberstar.Game.Key.Space, Amberstar.Game.KeyModifiers.None);
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "PictureText", 600), game.Game.ScriptState());
		Assert.Equal(65, game.Game.ScriptMapIndex);
	}

	[Fact]
	public void AmigaIntroShowsTheCreditsAndStartsTheGame()
	{
		using var game = new HeadlessGame(TestData.GetDataPath(Path.Combine("English", "AmberfilesAmiga")), showIntro: true);

		// the logo of Thalion is a picture which ends with a click
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "ThalionLogo"), game.Game.ScriptState());
		game.Step(60);
		Assert.Equal("ThalionLogo", game.Game.ScriptActiveScreen);
		game.Game.MouseDown(new(160, 100), Amberstar.Game.MouseButtons.Left, Amberstar.Game.KeyModifiers.None);
		game.Game.MouseUp(new(160, 100), Amberstar.Game.MouseButtons.Left, Amberstar.Game.KeyModifiers.None);
		// the title picture stays until a key is pressed
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "Intro", 300), game.Game.ScriptState());
		game.Step(120);
		Assert.Empty(game.Game.ScriptShownTexts);
		game.Game.KeyDown(Amberstar.Game.Key.Enter, Amberstar.Game.KeyModifiers.None);
		// the credits after the fading and the scrolling logo
		Assert.True(game.StepUntil(() => game.Game.ScriptShownTexts.Count == 1, 400), game.Game.ScriptState());
		Assert.Contains("Karsten", game.Game.ScriptShownTexts[0]);
		// any key ends the intro and the game starts with the graveyard text
		game.Game.KeyDown(Amberstar.Game.Key.Enter, Amberstar.Game.KeyModifiers.None);
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "PictureText", 600), game.Game.ScriptState());
	}

	[Fact]
	public void AmigaEscapeSkipsTheIntro()
	{
		using var game = new HeadlessGame(TestData.GetDataPath(Path.Combine("English", "AmberfilesAmiga")), showIntro: true);

		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "ThalionLogo"), game.Game.ScriptState());
		game.Game.KeyDown(Amberstar.Game.Key.Escape, Amberstar.Game.KeyModifiers.None);
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "PictureText", 600), game.Game.ScriptState());
		Assert.DoesNotContain(game.Game.ScriptShownTexts, text => text.Contains("Karsten"));
	}

	[Fact]
	public void TeleportToAnotherMapFadesInGameTime()
	{
		using var game = StartGame();

		Assert.Null(game.Game.ScriptTeleport(67, 7, 15));
		game.Step();
		Assert.True(game.Game.ScriptFading);

		// the fading takes 1 second of game time
		game.Step(90);

		Assert.False(game.Game.ScriptFading);
		Assert.True(game.Game.ScriptIdle, game.Game.ScriptState());
		Assert.Equal(67, game.Game.ScriptMapIndex);
		Assert.Equal(new Position(7, 15), game.Game.ScriptPartyPosition);
	}

	[Fact]
	public void PersonsCanOnlyBeTalkedToInTheirLanguage()
	{
		using var game = StartGame();

		// Spike (person 8) in the house of the family only knows the animal language.
		Assert.Null(game.Game.ScriptTeleport(69, 18, 10));
		game.Step(90);

		void TalkToSpike()
		{
			var screen = Assert.IsType<Amberstar.Game.Screens.Map2DScreen>(game.Game.ScreenHandler.ActiveScreen);
			var spike = screen.PresentCharacters.Single(character => character.CharacterIndex == 8);
			((Amberstar.Game.Screens.IMapScreen)screen).TriggerEvent(Amberstar.Game.Events.EventTrigger.Mouth, spike.Position.X, spike.Position.Y);
			game.Step(90);
		}

		TalkToSpike();
		Assert.Equal("TextBox", game.Game.ScriptActiveScreen);
		Assert.Contains(game.Game.ScriptShownTexts, text => text.Contains("VERSTEHT MAN DICH NICHT"));

		var result = game.RunScript(new ScenarioScript(["dismissall"], _ => { }), output: _ => { });
		Assert.True(result.Success, game.Game.ScriptState());

		// with the animal language the conversation starts
		game.Game.State.ActivePartyMember!.ConversationData.LearnedLanguages |= LanguageFlags.Animal;
		TalkToSpike();
		Assert.Equal("Conversation", game.Game.ScriptActiveScreen);
	}

	[Fact]
	public void HoldingAnArrowChangesTheAmountFasterAndFaster()
	{
		using var game = StartGame();
		var result = game.RunScript(new ScenarioScript(["setvalue 1 gold 5000", "click 32 17 right", "waitscreen Inventory", "button 7", "waitscreen PlaceGiveGold"], _ => { }), output: _ => { });
		Assert.True(result.Success, game.Game.ScriptState());

		var arrowUp = new Position(56, 168);
		game.Game.MouseMove(arrowUp, MouseButtons.None);
		game.Game.MouseDown(arrowUp, MouseButtons.Left, KeyModifiers.None);
		// the first click adds 1, then there is a short delay
		game.Step(15);
		Assert.Equal(1, game.Game.CurrentAmount);
		game.Step(60);
		int afterSecond = game.Game.CurrentAmount;
		Assert.True(afterSecond > 1);
		game.Step(60);
		int afterNextSecond = game.Game.CurrentAmount;
		Assert.True(afterNextSecond - afterSecond > afterSecond - 1);

		// it stops when the mouse button is released
		game.Game.MouseUp(arrowUp, MouseButtons.Left, KeyModifiers.None);
		game.Step(60);
		Assert.Equal(afterNextSecond, game.Game.CurrentAmount);
	}

	[Fact]
	public void DoorScreenDoesNotShowTheLastMessageAgain()
	{
		using var game = StartGame();

		bool MessageShown()
		{
			var screen = Assert.IsType<Amberstar.Game.Screens.DoorScreen>(game.Game.ScreenHandler.ActiveScreen);
			return screen.ScriptMessageShown;
		}

		void Run(params string[] commands)
		{
			var result = game.RunScript(new ScenarioScript(commands, _ => { }), output: _ => { });
			Assert.True(result.Success, game.Game.ScriptState());
		}

		// the door in the house of the family, the use item button shows a question
		Run("teleport 69 22 10 up", "dismissall", "move up 1", "waitscreen Door");
		Run("button 1", "waitscreen LockedUseItem");
		Run("click 160 100 right", "waitscreen Door", "button 2", "waitscreen Map2D");

		// the door is opened again
		Run("wait 1000", "waitidle", "move up 1", "waitscreen Door");
		Assert.False(MessageShown());
	}

	[Fact]
	public void TalkingToYourself()
	{
		using var game = StartGame();
		game.Game.ScriptClearShownTexts();

		var screen = Assert.IsType<Amberstar.Game.Screens.Map2DScreen>(game.Game.ScreenHandler.ActiveScreen);
		var position = game.Game.State.PartyPosition;
		Assert.True(((Amberstar.Game.Screens.IMapScreen)screen).TriggerEvent(Amberstar.Game.Events.EventTrigger.Mouth, position.X, position.Y));
		game.Step(30);

		Assert.Contains(game.Game.ScriptShownTexts, text => text.Contains("SELBSTGESPR"));
	}

	[Fact]
	public void PartySitsOnAChair()
	{
		using var game = StartGame();
		var assetProvider = TestData.CreateAssetProvider(GameSetup.RepositoryDataFolders[0]);

		// a chair in the inn "Zum tanzenden Drachen"
		var map = (IMap2D)assetProvider.MapLoader.LoadMap(71);
		var tileset = assetProvider.TilesetLoader.LoadTileset(map.TilesetIndex);
		int chair = Array.FindIndex(map.Tiles, tile => tile.Overlay != 0 && tileset.Tiles[tile.Overlay - 1].Flags.HasFlag(TileFlags.ChairOrBed));
		Assert.NotEqual(-1, chair);

		Assert.Null(game.Game.ScriptTeleport(71, chair % map.Width + 1, chair / map.Width + 1));
		game.Step(90);

		// the chair shows the sitting party, so the party itself is not drawn
		var screen = Assert.IsType<Amberstar.Game.Screens.Map2DScreen>(game.Game.ScreenHandler.ActiveScreen);
		Assert.False(screen.ScriptPartyVisible);
	}

	[Fact]
	public void EventBitsUseTheSaveIndexOfTheEvent()
	{
		using var game = StartGame();

		// Event 2 of the graveyard is the intro text with save index 1.
		Assert.True(game.Game.ScriptGetEventBit(65, 1));
		Assert.False(game.Game.ScriptGetEventBit(65, 2));
		Assert.False(game.Game.ScriptIsEventActive(65, 2));
		// Event 6 uses save index 2 and is not triggered yet.
		Assert.True(game.Game.ScriptIsEventActive(65, 6));
		// Events without save index are always active.
		Assert.True(game.Game.ScriptIsEventActive(65, 1));
	}

	[Fact]
	public void RandomMaximumIsInclusive()
	{
		G.SetRandomSeed(1);

		var values = Enumerable.Range(0, 200).Select(_ => G.Random(1, 3)).ToHashSet();

		Assert.Equal([1, 2, 3], values.Order());
	}

	[Fact]
	public void SameSeedGivesSameRandomNumbers()
	{
		G.SetRandomSeed(42);
		var first = Enumerable.Range(0, 20).Select(_ => G.Random(1, 100)).ToArray();
		G.SetRandomSeed(42);
		var second = Enumerable.Range(0, 20).Select(_ => G.Random(1, 100)).ToArray();

		Assert.Equal(first, second);
	}

	[Fact]
	public void MovementIsBlockedByWalls()
	{
		using var game = StartGame();

		var start = game.Game.ScriptPartyPosition;

		// The graveyard start position has a wall below and free space to the right.
		Assert.NotNull(game.Game.ScriptMove(0, 1));
		Assert.Equal(start, game.Game.ScriptPartyPosition);
		Assert.Null(game.Game.ScriptMove(1, 0));
		Assert.Equal(start + new Position(1, 0), game.Game.ScriptPartyPosition);
		Assert.Equal(Direction.Right, game.Game.ScriptPartyDirection);
	}

	[Fact]
	public void ItemsCanBeGiven()
	{
		using var game = StartGame();

		// item 1 is used by some character or chest
		int before = game.Game.ScriptCountItem(1);
		Assert.Null(game.Game.ScriptGiveItem(1, 1, 1));
		Assert.Equal(before + 1, game.Game.ScriptCountItem(1));
	}
}
