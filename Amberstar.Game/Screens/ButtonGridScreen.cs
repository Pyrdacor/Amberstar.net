using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class ButtonGridScreen : Screen
{
    ButtonGrid? buttonGrid;

	protected abstract void SetupButtons(ButtonGrid buttonGrid);
	protected abstract void ButtonClicked(int index);
	/// <summary>
	/// The map screens move the party while these keys are held, they don't click a button when they are released.
	/// </summary>
	protected virtual bool KeypadMovesParty(Key key) => false;
    internal abstract byte ButtonGridPaletteIndex { get; }
    internal void UpdateButtons() => RequestButtonSetup();
    protected void RequestButtonSetup()
    {
        // The grid does not exist while another screen covers this one.
        if (buttonGrid != null)
            SetupButtons(buttonGrid);
    }
    protected void RequestButtonGridPaletteUpdate() => buttonGrid!.PaletteIndex = ButtonGridPaletteIndex;

    public ButtonType GetButtonType(int index) => buttonGrid?.GetButtonType(index) ?? ButtonType.Empty;

    /// <summary>
    /// Presses a button like a click (used by scenario scripts).
    /// Returns false if the button is disabled or empty.
    /// </summary>
    internal bool ScriptPressButton(int index)
    {
        if (buttonGrid == null || !buttonGrid.IsButtonEnabled(index) || buttonGrid.GetButtonType(index) is ButtonType.Empty or ButtonType.EmptyArea)
            return false;

        buttonGrid.Press(index);

        return true;
    }

    internal bool ScriptIsButtonEnabled(int index) => buttonGrid != null && buttonGrid.IsButtonEnabled(index) && buttonGrid.GetButtonType(index) is not (ButtonType.Empty or ButtonType.EmptyArea);

    public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

        if (buttonGrid == null)
        {
            buttonGrid = new(Game)
            {
                PaletteIndex = ButtonGridPaletteIndex
            };
            buttonGrid.ClickButtonAction += ButtonClicked;
        }

        SetupButtons(buttonGrid);
    }

	public override void Close()
	{
        if (buttonGrid != null)
        {
            buttonGrid.ClickButtonAction -= ButtonClicked;
            buttonGrid.Destroy();
            buttonGrid = null;
        }

        base.Close();
    }

	public override void ScreenPushed(Screen screen)
	{
		if (!screen.Transparent && buttonGrid != null)
        {
			buttonGrid.ClickButtonAction -= ButtonClicked;
            buttonGrid.Destroy();
			buttonGrid = null;
        }

        base.ScreenPushed(screen);
    }

	public override void ScreenPopped(Screen screen)
	{
        if (!screen.Transparent)
        {
			if (buttonGrid == null)
			{
                buttonGrid = new(Game)
                {
                    PaletteIndex = ButtonGridPaletteIndex
                };
                buttonGrid.ClickButtonAction += ButtonClicked;
			}

            SetupButtons(buttonGrid);
        }

        base.ScreenPopped(screen);
    }

	public override bool KeyUp(Key key, KeyModifiers keyModifiers)
	{
		if (keyModifiers == KeyModifiers.None && key >= Key.Keypad1 && key <= Key.Keypad9)
		{
			if (KeypadMovesParty(key))
				return false;

			// The keys are arranged like the buttons, so the row of 7, 8 and 9 is the upper one.
			int keyIndex = (int)key - (int)Key.Keypad1;
			int buttonIndex = (2 - keyIndex / 3) * 3 + keyIndex % 3;

            if (buttonGrid?.IsButtonEnabled(buttonIndex) == true)
                ButtonClicked(buttonIndex);

            return true;
		}

        return base.KeyUp(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttonGrid?.MouseClick(position) == true)
            return true;

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
