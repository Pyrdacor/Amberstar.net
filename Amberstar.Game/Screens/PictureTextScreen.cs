using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class PictureTextScreen : Screen
{
	const int TextX = 112;
	const int TextY = 50;
	const int TextWidth = 192;
	const int TextHeight = 140;
    const int ControlDisplayLayer = 100;
	readonly TextScrollHandler textScrollHandler = new();
    Image? image;
	Label? displayText;
	bool closeOnNextInput = false;

	public override ScreenType Type { get; } = ScreenType.PictureText;

    public override void Init()
    {
        base.Init();

        textScrollHandler.ScrollEnded += () => Game.ScreenHandler.PopScreen();
    }

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Game.Cursor.CursorType = CursorType.Zzz;

        Image80x80 imageType;
        IText text;

        if (Game.CurrentPictureText is var (picture, pictureText))
        {
            imageType = picture;
            text = pictureText;
        }
        else
        {
            var @event = (Game.EventHandler.CurrentEvent as IShowPictureTextEvent)!;
            imageType = (Image80x80)@event.Picture;
            int mapIndex = Game.EventHandler.CurrentEventMapIndex;
            text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex)).GetTextBlock(@event.TextIndex);
        }

        var palette = Game.PaletteIndexProvider.Get80x80ImagePaletteIndex(imageType);
        
		image = AddImage(16, 81, 80, 80, Game.GraphicIndexProvider.Get80x80ImageIndex(imageType), ControlDisplayLayer, true);
		image.PaletteIndex = palette;
		image.Visible = true;

        Game.SetLayout(Layout.PictureText, palette);
		Game.ScriptTextShown(text);

		displayText = AddLabel(TextX, TextY, text, TextWidth, TextHeight, ControlDisplayLayer);
		displayText.PaletteIndex = palette;
		closeOnNextInput = !displayText.SupportsScrolling;

        textScrollHandler.Attach(displayText);
    }

    public override void Close()
    {
        Game.Cursor.CursorType = CursorType.Sword;
		textScrollHandler.Detach();

        base.Close();
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        // Other keys must not close the text (e.g. movement keys while the party walks).
        if (!IsTextConfirmKey(key))
            return true;

        if (closeOnNextInput)
        {
            closeOnNextInput = false;
            Game.ScreenHandler.PopScreen();
            return true;
        }

        return textScrollHandler.KeyDown(key, keyModifiers) || base.KeyDown(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (closeOnNextInput)
        {
            closeOnNextInput = false;
            Game.ScreenHandler.PopScreen();
            return true;
        }

        return textScrollHandler.MouseDown(buttons) || base.MouseDown(position, buttons, keyModifiers);
    }
}
