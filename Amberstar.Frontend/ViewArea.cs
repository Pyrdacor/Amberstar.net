namespace Amberstar.Frontend;

/// <summary>
/// The part of a window which shows the game. The aspect ratio of the game stays
/// the same, so there are black bars if the window has another one.
/// </summary>
public readonly record struct ViewArea(int X, int Y, int Width, int Height)
{
	const int VirtualWidth = 320;
	const int VirtualHeight = 200;

	public static ViewArea Calculate(int windowWidth, int windowHeight)
	{
		float scale = Math.Min((float)windowWidth / VirtualWidth, (float)windowHeight / VirtualHeight);
		int width = Math.Min(windowWidth, (int)MathF.Round(VirtualWidth * scale));
		int height = Math.Min(windowHeight, (int)MathF.Round(VirtualHeight * scale));

		return new((windowWidth - width) / 2, (windowHeight - height) / 2, width, height);
	}

	/// <summary>
	/// Converts a window position to a position of the virtual screen. Positions on the bars end at the border of the game.
	/// </summary>
	public (float X, float Y) ToVirtual(float x, float y) =>
	(
		Math.Clamp((x - X) * VirtualWidth / Width, 0.0f, VirtualWidth - 1),
		Math.Clamp((y - Y) * VirtualHeight / Height, 0.0f, VirtualHeight - 1)
	);

	public (float X, float Y) FromVirtual(float x, float y) => (X + x * Width / VirtualWidth, Y + y * Height / VirtualHeight);
}
