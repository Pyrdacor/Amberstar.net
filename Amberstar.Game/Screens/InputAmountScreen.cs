using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class InputAmountScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 80+32;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 150;
    const byte ControlDisplayLayer = 175;
    Label? inputValue;
    Image? image;
    Label? inputLabel;
    Label? messageLabel;
    Button? upButton;
    Button? downButton;
    Button? dropButton;
    (Message Message, Action<int> Dropped)? drop;
    // Holding the left mouse button on an arrow changes the amount after a delay and faster and faster.
    // The high byte of this value is added per vertical blank (Adding and ItemW_Up in the original).
    const int FirstAdding = 256 - 8 * 20;
    const int AddingStep = 8;
    const int MaxAdding = 0x7fff;
    const int VerticalBlanksPerSecond = 50;
    int adding = FirstAdding;
    int heldDirection = 0;
    long verticalBlankTicks = 0;
    Position mousePosition = new();

    protected abstract ItemGraphic Graphic { get; }

    protected abstract IText InputLabelText { get; }

    protected abstract Message Message { get; }

    public override bool CloseOnEscape { get; } = false;

    public override bool UpdateWhilePaused { get; } = true;

    /// <summary>
    /// If true, the graphic and the texts are updated whenever the
    /// window is opened (e.g. for the amount of different items).
    /// </summary>
    protected virtual bool DynamicContent => false;

    const int MaxDynamicTextWidth = 128;

    public override void Init()
    {
        base.Init();

        Label CreateLabel(int x, int y, int width, IText? text = null)
        {
            var label = AddLabel(x, y, width, 7, ControlDisplayLayer);

            if (text != null)
                label.SetText(text, width);

            return label;
        }

        Label CreateFixedLabel(int x, int y, IText text) => CreateLabel(x, y, DynamicContent ? MaxDynamicTextWidth : Game.GetMaxLineWidth(text), text);

        // the positions are relative to the client area of the window
        int x = 0;
        int y = 0;

        // Image background
        AddImage(x, y, 16, 16, Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), ControlDisplayLayer, true);

        image = AddImage(x, y, 16, 16, Game.GraphicIndexProvider.GetItemGraphicIndex(Graphic), ControlDisplayLayer + 5);
        image.PaletteIndex = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item];

        x += 16 + 3;
        y += 2;

        inputLabel = CreateFixedLabel(x, y, InputLabelText);
        inputLabel.Visible = true;

        y += 11;

        inputValue = CreateLabel(x, y, 52);
        inputValue.Alignment = TextAlignment.Right;

        x = 1;
        y += 9;

        var text = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)Message));
        messageLabel = CreateFixedLabel(x, y, text);
        messageLabel.Visible = true;

        x = 0;
        y = 32;

        upButton = AddButton(x, y, ButtonType.ArrowUp, ControlDisplayLayer);
        upButton.ClickAction += () => ChangeAmount(1);
        upButton.RightClickAction += () => ChangeAmount(short.MaxValue);
        downButton = AddButton(x, y + 16, ButtonType.ArrowDown, ControlDisplayLayer);
        downButton.ClickAction += () => ChangeAmount(-1);
        downButton.RightClickAction += () => ChangeAmount(short.MinValue);

        dropButton = AddButton(x + 80, y, ButtonType.DropItem, ControlDisplayLayer);
        dropButton.ClickAction += Drop;

        // the exit icon is at 128,176 like in the original (ItemW_DisInit)
        var exitButton = AddButton(x + 80, y + 16, ButtonType.Exit, ControlDisplayLayer);
        exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();
    }

    private void ChangeAmount(int change)
    {
        if (change == 0)
            return;

        Game.CurrentAmount = MathUtil.Limit(0, Game.CurrentAmount + change, Game.CurrentMaxAmount);

        UpdateControls();
    }

    /// <summary>
    /// Asks if the entered amount should be thrown away. Then this window is also closed.
    /// </summary>
    private void Drop()
    {
        int amount = Game.CurrentAmount;

        if (amount == 0 || drop is not { } dropInfo)
            return;

        Game.AskForConfirmation(Game.LoadMessageText(dropInfo.Message), () =>
        {
            Game.CurrentAmount = 0;
            Game.ScreenHandler.PopScreen();
            dropInfo.Dropped(amount);
        });
    }

    private void VerticalBlankWhileHeld()
    {
        var button = heldDirection > 0 ? upButton! : downButton!;

        if (!button.Area.Contains(mousePosition))
            return;

        int amount = Game.CurrentAmount + heldDirection * (adding >> 8);
        int limit = heldDirection > 0 ? Game.CurrentMaxAmount : 0;

        if (Game.CurrentAmount == limit)
            return;

        if (heldDirection > 0 ? amount >= limit : amount < limit)
        {
            adding = FirstAdding;
            amount = limit;
        }

        adding = Math.Min(adding + AddingStep, MaxAdding);
        Game.CurrentAmount = amount;
        UpdateControls();
    }

    public override void Update(long elapsedTicks)
    {
        base.Update(elapsedTicks);

        if (heldDirection == 0)
            return;

        verticalBlankTicks += elapsedTicks * VerticalBlanksPerSecond;

        for (; verticalBlankTicks >= Game.TicksPerSecond; verticalBlankTicks -= Game.TicksPerSecond)
            VerticalBlankWhileHeld();
    }

    private void ReleaseArrow()
    {
        heldDirection = 0;
        adding = FirstAdding;
        verticalBlankTicks = 0;
    }

    private void UpdateControls()
    {
        inputValue!.SetText(Game.CurrentAmount.ToString());

        upButton!.Disabled = Game.CurrentAmount == Game.CurrentMaxAmount;
        downButton!.Disabled = Game.CurrentAmount == 0;
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentMaxAmount <= 0)
			throw new InvalidOperationException($"{nameof(InputAmountScreen)} needs {nameof(Game.CurrentMaxAmount)} to be set beforehand.");

		base.Open(closeAction);

        ReleaseArrow();
        drop = Game.CurrentAmountDrop;
        Game.CurrentAmountDrop = null;
        dropButton!.Visible = drop != null;

        if (DynamicContent)
        {
            image!.SetTextureIndex(Game.GraphicIndexProvider.GetItemGraphicIndex(Graphic));
            inputLabel!.SetText(InputLabelText, MaxDynamicTextWidth);
            messageLabel!.SetText(Game.LoadMessageText(Message), MaxDynamicTextWidth);
        }

        UpdateControls();
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        switch (key)
        {
            case Key.Up:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(1);
                else
                    ChangeAmount(Game.CurrentMaxAmount - Game.CurrentAmount);
                return true;
            case Key.Down:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(-1);
                else
                    ChangeAmount(-Game.CurrentAmount);
                return true;
            case Key.PageUp:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(10);
                else
                    ChangeAmount(Game.CurrentMaxAmount - Game.CurrentAmount);
                return true;
            case Key.PageDown:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(-10);
                else
                    ChangeAmount(-Game.CurrentAmount);
                return true;
            case Key.Escape:
                Game.CurrentAmount = 0;
                Game.ScreenHandler.PopScreen();
                return true;
            case Key.Space:
            case Key.Enter:
                Game.ScreenHandler.PopScreen();
                return true;
        }

        return false;
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        ReleaseArrow();
        mousePosition = position;

        if (keyModifiers != KeyModifiers.None)
            buttons = MouseButtons.Right;
        else if (buttons == MouseButtons.Left)
            heldDirection = upButton!.Area.Contains(position) ? 1 : downButton!.Area.Contains(position) ? -1 : 0;

        return base.MouseDown(position, buttons, keyModifiers);
    }

    public override bool MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
    {
        ReleaseArrow();

        return base.MouseUp(position, buttons, keyModifiers);
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        mousePosition = position;

        base.MouseMove(position, buttons);
    }
}
