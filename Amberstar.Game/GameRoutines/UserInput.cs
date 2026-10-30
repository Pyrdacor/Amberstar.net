using Amber.Common;
using Amberstar.Game.Screens;
using Amberstar.Game.UI;

namespace Amberstar.Game;

partial class Game
{
    readonly Action<Position> setMousePosition;
    readonly Func<List<Key>> pressedKeyProvider;	
    List<Key>? pressedKeys = null;
    Rect? mouseTrapArea = null;
	Position lastMousePosition = new();

    internal bool InputEnabled { get; private set; } = true;		
	internal bool Paused { get; private set; } = false;

	// The password of the cheat mode of the original (stored encrypted in the program, checked at $1086).
	const string CheatPassword = "TEMPUS FUGIT";
	int cheatPasswordPosition = 0;

	internal bool CheatMode { get; private set; } = false;

	/// <summary>
	/// The cheat mode is activated by typing its password anywhere. A wrong key starts it again.
	/// </summary>
	void CheckCheatPassword(char ch)
	{
		if (CheatMode)
			return;

		ch = char.ToUpperInvariant(ch);

		if (ch != CheatPassword[cheatPasswordPosition])
			cheatPasswordPosition = ch == CheatPassword[0] ? 1 : 0;
		else if (++cheatPasswordPosition == CheatPassword.Length)
		{
			CheatMode = true;

			if (ScreenHandler.ActiveScreen is IMapScreen)
				ShowTextMessage(TextManager.ToGameCharacters(HasEnglishTexts ? "CHEAT MODE ON. D SELECTS THE MUSIC." : "CHEATMODUS AN. D WÄHLT DIE MUSIK."));
		}
	}

	/// <summary>
	/// The key which produced the character of the following key char event.
	/// </summary>
	internal Key LastKeyDown { get; private set; } = Key.Invalid;

	public void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		LastKeyDown = key;

		if (key == Key.F10)
			PlaySong(1);
		else if (key == Key.F5 && SavegamePath != null && ScreenHandler.ActiveScreen is IMapScreen)
		{
			// quick save
			SaveGame();
			ShowTextMessage($"Game was saved at '{Path.GetFileName(SavegamePath)}'.");
			return;
		}
        else if (key == Key.F7 && HasSavegame && ScreenHandler.ActiveScreen is IMapScreen)
        {
            // quick load
            LoadGameAndContinue();
            return;
        }

        if (!InputEnabled)
			return;

		// [ D ] selects the music in the cheat mode of the original.
		if (CheatMode && key == Key.LetterA + 3 && ScreenHandler.ActiveScreen is IMapScreen)
		{
			ScreenHandler.PushScreen(ScreenType.Jukebox);
			return;
		}

		if (ScreenHandler.ActiveScreen?.KeyDown(key, keyModifiers) == true)
			return;
	}

	public void KeyUp(Key key, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.KeyUp(key, keyModifiers) == true)
			return;
	}

	public void KeyChar(char ch, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		CheckCheatPassword(ch);

		if (ScreenHandler.ActiveScreen?.KeyChar(ch, keyModifiers) == true)
			return;
	}

	public void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.MouseDown(position, buttons, keyModifiers) == true)
			return;
	}

	public void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.MouseUp(position, buttons, keyModifiers) == true)
			return;
	}

	public void MouseMove(Position position, MouseButtons buttons)
	{
        if (mouseTrapArea != null)
		{
			var newPosition = new Position
			(
				MathUtil.Limit(mouseTrapArea.Value.Left, position.X, mouseTrapArea.Value.Right - 1),
				MathUtil.Limit(mouseTrapArea.Value.Top, position.Y, mouseTrapArea.Value.Bottom - 1)
			);

			if (position != newPosition)
			{
				position = newPosition;
				setMousePosition(position);
			}
        }
		else
		{
			// The mouse may leave the window, the cursor stays at the border then.
            position = new Position
            (
                MathUtil.Limit(0, position.X, VirtualScreenWidth - 1),
                MathUtil.Limit(0, position.Y, VirtualScreenHeight - 1)
            );
        }

        lastMousePosition = position;
        Cursor.Position = position;

		if (!InputEnabled)
			return;

		ScreenHandler.ActiveScreen?.MouseMove(position, buttons);
	}

	public void MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.MouseWheel(position, scrollX, scrollY, buttons) == true)
			return;
    }

    public void TrapMouse(Rect rect)
    {
		mouseTrapArea = rect;

		var position = lastMousePosition;

		position = new Position(MathUtil.Limit(rect.Left, position.X, rect.Right - 1), MathUtil.Limit(rect.Top, position.Y, rect.Bottom - 1));

		if (position != lastMousePosition)
		{
            setMousePosition(position);
        }
    }

	internal void TrapMouseInPortraitArea() => TrapMouse(PortraitArea);

	public void UntrapMouse()
	{
		mouseTrapArea = null;
		ScreenHandler.ActiveScreen?.MouseMove(lastMousePosition, MouseButtons.None);
    }

	public void SimulateMouseMoveWithoutButton() => MouseMove(lastMousePosition, MouseButtons.None);

    internal bool IsKeyDown(Key key) => InputEnabled && (pressedKeys ??= pressedKeyProvider()).Contains(key);

	internal bool IsKeyDown(char ch) => InputEnabled && (pressedKeys ??= pressedKeyProvider()).Contains(KeyByChar(ch));

	private static Key KeyByChar(char ch)
	{
		if (ch >= '0' && ch <= '9')
			return Key.Number0 + ch - '0';
		else if (ch >= 'A' && ch <= 'Z')
			return Key.LetterA + ch - 'A';
		else if (ch >= 'a' && ch <= 'z')
			return Key.LetterA + ch - 'a';
		else if (ch == '\n')
			return Key.Enter;
		else if (ch == ' ')
			return Key.Space;
		else
			return Key.Invalid;
	}

	internal void Pause()
	{
		Paused = true;
	}

	internal void Resume()
	{
		Paused = false;
	}

	internal void EnableInput(bool enable)
	{
		InputEnabled = enable;
	}
}
