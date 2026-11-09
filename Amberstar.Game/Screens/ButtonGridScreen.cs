using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class ButtonGridScreen : Screen
{
    ButtonGrid? buttonGrid;
	// Like in the original some buttons are clicked again and again while they are held (Zzz on maps).
	readonly HoldRepeater buttonRepeater = new();
	int? heldButtonIndex = null;
	bool buttonHeldByKey = false;

	protected abstract void SetupButtons(ButtonGrid buttonGrid);
	protected abstract void ButtonClicked(int index);
	/// <summary>
	/// The map screens move the party while these keys are held, they don't click a button when they are released.
	/// </summary>
	protected virtual bool KeypadMovesParty(Key key) => false;
	/// <summary>
	/// The button is clicked repeatedly while the mouse button or its key on the numeric keypad is held.
	/// </summary>
	protected virtual bool RepeatsWhileHeld(int index) => false;
	/// <summary>
	/// Called when the mouse button is held long enough on a button or released (e.g. to move while an arrow button is held).
	/// </summary>
	protected virtual void HeldButtonChanged() { }
	/// <summary>
	/// The given key of the numeric keypad is held or the mouse button is held on the button at the same place.
	/// </summary>
	protected bool IsKeypadKeyDown(Key key) => Game.IsKeyDown(key) || (HeldButtonIndex != null && HeldButtonIndex == GetKeypadButtonIndex(key));
	/// <summary>
	/// The button on which the mouse button is held long enough (a short click only clicks the button).
	/// </summary>
	protected int? HeldButtonIndex => buttonHeldByKey || !buttonRepeater.LongHeld ? null : heldButtonIndex;
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
		ReleaseButton();

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

	/// <summary>
	/// Switches between the movement and the action buttons on maps (right click on the buttons or enter).
	/// </summary>
	protected void ToggleButtonLayout()
	{
		Game.ButtonLayout = (ButtonLayout)(1 - (int)Game.ButtonLayout);
		RequestButtonSetup();
	}

	// The keys are arranged like the buttons, so the row of 7, 8 and 9 is the upper one.
	static int GetKeypadButtonIndex(Key key)
	{
		int keyIndex = (int)key - (int)Key.Keypad1;

		return (2 - keyIndex / 3) * 3 + keyIndex % 3;
	}

	private void HoldButton(int index, bool byKey)
	{
		// the key repeat of the system must not restart the hold
		if (heldButtonIndex == index && buttonHeldByKey == byKey)
			return;

		// keys are only tracked to repeat their buttons
		if (byKey && !RepeatsWhileHeld(index))
			return;

		heldButtonIndex = index;
		buttonHeldByKey = byKey;
		buttonRepeater.Reset();
	}

	private void ReleaseButton()
	{
		if (heldButtonIndex == null)
			return;

		bool longHeld = buttonRepeater.LongHeld;
		heldButtonIndex = null;
		buttonRepeater.Reset();

		if (longHeld)
			HeldButtonChanged();
	}

	/// <summary>
	/// Clicks a held button again after some time. The screens call this in Update.
	/// </summary>
	protected void UpdateHeldButton(long elapsedTicks)
	{
		if (heldButtonIndex is not int index)
			return;

		if (buttonGrid?.IsButtonEnabled(index) != true || Game.Paused || !Game.InputEnabled)
		{
			ReleaseButton();
			return;
		}

		bool longHeld = buttonRepeater.LongHeld;

		if (buttonRepeater.Update(elapsedTicks) && RepeatsWhileHeld(index))
			buttonGrid.Press(index);

		if (!longHeld && buttonRepeater.LongHeld)
			HeldButtonChanged();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (keyModifiers == KeyModifiers.None && key >= Key.Keypad1 && key <= Key.Keypad9 && !KeypadMovesParty(key))
		{
			int buttonIndex = GetKeypadButtonIndex(key);

			if (buttonGrid?.IsButtonEnabled(buttonIndex) == true)
				HoldButton(buttonIndex, true);
		}

		return base.KeyDown(key, keyModifiers);
	}

	public override bool KeyUp(Key key, KeyModifiers keyModifiers)
	{
		if (keyModifiers == KeyModifiers.None && key >= Key.Keypad1 && key <= Key.Keypad9)
		{
			if (KeypadMovesParty(key))
				return false;

			int buttonIndex = GetKeypadButtonIndex(key);
			bool repeated = buttonHeldByKey && heldButtonIndex == buttonIndex && buttonRepeater.Repeated;

			if (buttonHeldByKey)
				ReleaseButton();

			// the button was already clicked while the key was held
			if (!repeated && buttonGrid?.IsButtonEnabled(buttonIndex) == true)
                ButtonClicked(buttonIndex);

            return true;
		}

        return base.KeyUp(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		var buttonIndex = buttons == MouseButtons.Left ? buttonGrid?.GetButtonIndex(position) : null;

		if (buttonGrid?.MouseClick(position) == true)
		{
			if (buttonIndex != null)
				HoldButton(buttonIndex.Value, false);

            return true;
		}

        return base.MouseDown(position, buttons, keyModifiers);
    }

	public override bool MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (!buttonHeldByKey)
			ReleaseButton();

		return base.MouseUp(position, buttons, keyModifiers);
	}

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		// moving the mouse away from the button releases it
		if (heldButtonIndex != null && !buttonHeldByKey && buttonGrid?.GetButtonIndex(position) != heldButtonIndex)
			ReleaseButton();

		base.MouseMove(position, buttons);
	}
}
