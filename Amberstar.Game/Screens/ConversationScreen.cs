using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal class ConversationScreen : Screen
{
	Game? game;

	public override ScreenType Type { get; } = ScreenType.Conversation;

	public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		this.game = game;

        var palette = game.PaletteIndexProvider.UIPaletteIndex;

        game.SetLayout(Layout.Conversation, palette);
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
