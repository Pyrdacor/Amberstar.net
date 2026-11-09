namespace Amberstar.Game.UI;

/// <summary>
/// Repeats an action while a mouse button or key is held, like the original does for
/// the movement buttons and Zzz (the mouse events are handled as long as the button is held).
/// </summary>
internal sealed class HoldRepeater
{
	const long FirstRepeatTicks = Game.TicksPerSecond * 3 / 10;
	const long RepeatTicks = Game.TicksPerSecond / 10;
	long heldTicks = 0;
	int repeats = 0;

	/// <summary>
	/// The action was repeated since the last reset.
	/// </summary>
	public bool Repeated => repeats != 0;

	/// <summary>
	/// It is held long enough to repeat the action (a short click only executes it once).
	/// </summary>
	public bool LongHeld => heldTicks >= FirstRepeatTicks;

	public void Reset()
	{
		heldTicks = 0;
		repeats = 0;
	}

	/// <summary>
	/// Returns true if the action should be repeated now.
	/// </summary>
	public bool Update(long elapsedTicks)
	{
		heldTicks += elapsedTicks;

		if (heldTicks < FirstRepeatTicks + repeats * RepeatTicks)
			return false;

		repeats++;

		return true;
	}
}
