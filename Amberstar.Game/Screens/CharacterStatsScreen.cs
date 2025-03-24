using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class CharacterStatsScreen : ButtonGridScreen
{
	Game? game;
    IPartyMember? partyMember;
    PersonInfoView? personInfoView;

    public override ScreenType Type { get; } = ScreenType.CharacterStats;

    protected override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

    protected override void SetupButtons(ButtonGrid buttonGrid)
    {
        if (partyMember == null)
            return;

        // Upper row
        buttonGrid.SetButton(0, ButtonType.Inventory);
        buttonGrid.SetButton(2, ButtonType.Exit);
    }

    public override void Open(Game game, Action? closeAction)
	{
        this.game = game;

		base.Open(game, closeAction);

        var palette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        game.SetLayout(Layout.Stats, palette);
        game.Cursor.CursorType = CursorType.Sword;

        SwitchToPartyMember(game.State.CurrentInventoryIndex!.Value, true);
    }

    public override void Close(Game game)
    {
        personInfoView?.Destroy();

        base.Close(game);
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;
        }

        base.ScreenPushed(game, screen);
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = true;
        }
    }

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (key == Key.Escape)
            game!.ScreenHandler.PopScreen();

        base.KeyDown(key, keyModifiers);
	}

    public void SwitchToPartyMember(int index, bool force)
    {
        if (!force && game!.State.CurrentInventoryIndex == index)
            return;

        game!.State.CurrentInventoryIndex = index;
        int partyMemberIndex = 1; // TODO: get from savegame, slot is index
        partyMember = (game!.AssetProvider.PersonLoader.LoadPerson(partyMemberIndex) as IPartyMember)!;
        var graphicLoader = game!.AssetProvider.GraphicLoader;
        var uiPaletteIndex = game!.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        personInfoView?.Destroy();
        personInfoView = new(game, partyMember, partyMemberIndex, uiPaletteIndex);

        RequestButtonSetup();
    }

    protected override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Stats
                game?.ScreenHandler.PopScreen();
                game?.ScreenHandler.PushScreen(ScreenType.Inventory);
                break;
            case 2:
                game?.ScreenHandler.PopScreen();
                break;
        }
    }
}
