using Amber.Assets.Common;

namespace Amberstar.GameData;

/// <summary>
/// The graphics of the intro. The title pictures (one per fading step) have their own colors, the others use the
/// palette of the background.
/// </summary>
public enum IntroGraphic
{
	TitlePicture,
	Background,
	Logo,
	TextPage
}

/// <summary>
/// The intro (INTRO.68K, a separate program of the original). The title picture is shown until the space key
/// is pressed. Then the credits are shown page by page on a stone background with the logo of the game while
/// the music plays, until the space key is pressed.
///
/// On the Amiga the loader shows the title picture (INTRO.UDO) without fading and the credits are the program
/// INTRO_P.UDO. It is the same as on the Atari ST except for the timing, the logo which scrolls in and the music
/// which starts after the logo. Any key or the mouse button continues.
/// </summary>
public interface IIntroData
{
	public const int PictureWidth = 320;
	public const int PictureHeight = 200;

	/// <summary>
	/// The title picture (RGBA) for each fading step from dark (0) to the normal picture
	/// (<see cref="TitleFadeSteps"/> - 1). It has 48 colors per line like the pictures of Spectrum 512
	/// (the palette is changed 3 times in each line). The Amiga has only the normal picture (it is not faded).
	/// </summary>
	IReadOnlyList<IGraphic> TitlePictures { get; }
	public const int TitleFadeSteps = 7;

	/// <summary>
	/// The stone background (320x200).
	/// </summary>
	IGraphic Background { get; }

	/// <summary>
	/// Gets the palette of the background while it fades: 0 is the normal palette, 1 to <see cref="FadeSteps"/> - 1
	/// fade in from black (each color channel is increased by 1 per step), -1 to -<see cref="FadeSteps"/> fade out
	/// (each channel is decreased by 1 per step).
	/// </summary>
	IGraphic GetBackgroundPalette(int fadeStep = 0);

	/// <summary>
	/// The steps of the fading (7 with 3 vertical blanks each on the Atari ST, 15 with 2 vertical blanks each on the Amiga).
	/// </summary>
	int FadeSteps { get; }
	int VerticalBlanksPerFadeStep { get; }

	/// <summary>
	/// The logo of the game (color 0 is transparent). On the Atari ST it is drawn line by line at <see cref="LogoY"/>
	/// (320x36), on the Amiga it scrolls up from the bottom to <see cref="LogoY"/> (320x33, 2 lines per vertical blank).
	/// </summary>
	IGraphic Logo { get; }
	int LogoY { get; }
	bool LogoScrollsIn { get; }

	/// <summary>
	/// On the Amiga the music starts after the logo, on the Atari ST before it.
	/// </summary>
	bool MusicStartsAfterLogo { get; }

	/// <summary>
	/// The width of the blocks which the second transition of the pages shows per vertical blank (16 on the Atari ST,
	/// 8 on the Amiga).
	/// </summary>
	int TransitionBlockWidth { get; }

	/// <summary>
	/// On the Atari ST only the space key continues, on the Amiga any key (the mouse button on both).
	/// </summary>
	bool AnyKeyContinues { get; }

	/// <summary>
	/// The texts of the credits (lines separated by '\n').
	/// </summary>
	IReadOnlyList<string> Texts { get; }

	/// <summary>
	/// The credit pages: the part of the background from <see cref="TextAreaY"/> with the text of the page.
	/// </summary>
	IReadOnlyList<IGraphic> TextPages { get; }
	public const int TextAreaY = 75;
	public const int TextAreaHeight = 112;

	ISong? Music { get; }
}
