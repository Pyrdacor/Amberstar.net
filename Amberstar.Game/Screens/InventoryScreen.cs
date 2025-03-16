using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal class InventoryScreen : Screen
{
	Game? game;

	public override ScreenType Type { get; } = ScreenType.Inventory;

	public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		this.game = game;

        var palette = game.PaletteIndexProvider.UIPaletteIndex;

        game.SetLayout(Layout.Inventory, palette);
	}

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		game!.ScreenHandler.PopScreen();
	}

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        game!.ScreenHandler.PopScreen();
    }
}
