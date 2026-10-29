using Amber.Assets.Common;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The logo of Thalion of the Amiga (TH_LOGO.UDO, shown by the loader). It is a picture with 5 bit planes (the planes
/// of each line after another) which is shown until a key or the mouse button is pressed. There is no music.
/// The escape key skips the intro (the loader checks the key code $45).
/// </summary>
internal sealed class AmigaThalionLogo : IThalionLogo
{
	const int Planes = 5;
	const int PlaneLineSize = IThalionLogo.Width / 8;
	// the scan code of the escape key (Atari ST keyboard)
	const int EscapeScanCode = 0x01;
	// the palette is in the loader (L2ea, the program "Amberstar" on disk A)
	static readonly int[] Palette =
	[
		0x000, 0x112, 0xdd4, 0xc90, 0x301, 0xa00, 0x701, 0xcce, 0xaae, 0x88e, 0x004, 0x006, 0x44c, 0x008, 0x22a, 0x66c,
		0x002, 0xb41, 0xb71, 0x536, 0x222, 0x333, 0x444, 0x555, 0x666, 0x777, 0x888, 0x99a, 0xaab, 0x324, 0x314, 0x203
	];

	public AmigaThalionLogo(byte[] picture)
	{
		var rgba = new byte[IThalionLogo.Width * IThalionLogo.Height * 4];

		for (int y = 0; y < IThalionLogo.Height; y++)
		{
			for (int x = 0; x < IThalionLogo.Width; x++)
			{
				int color = 0;

				for (int plane = 0; plane < Planes; plane++)
				{
					if ((picture[(y * Planes + plane) * PlaneLineSize + x / 8] & (0x80 >> (x % 8))) != 0)
						color |= 1 << plane;
				}

				int value = Palette[color];
				int target = (y * IThalionLogo.Width + x) * 4;
				rgba[target] = (byte)(((value >> 8) & 0xf) * 17);
				rgba[target + 1] = (byte)(((value >> 4) & 0xf) * 17);
				rgba[target + 2] = (byte)((value & 0xf) * 17);
				rgba[target + 3] = 0xff;
			}
		}

		Frame = Graphic.FromRGBA(IThalionLogo.Width, IThalionLogo.Height, rgba);
	}

	public bool Finished { get; private set; }

	public bool SkipsIntro { get; private set; }

	public IGraphic Frame { get; }

	public int Song => 0;

	public ISong GetSong(int song) => throw new NotSupportedException("The logo of the Amiga has no music.");

	public void Reset()
	{
		Finished = false;
		SkipsIntro = false;
	}

	public void VerticalBlank()
	{
	}

	public void KeyPressed(int scanCode)
	{
		Finished = true;
		SkipsIntro = scanCode == EscapeScanCode;
	}

	public void KeyReleased()
	{
	}

	public void Skip() => Finished = true;
}
