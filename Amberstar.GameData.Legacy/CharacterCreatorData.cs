namespace Amberstar.GameData.Legacy;

/// <summary>
/// The graphics of the character editor (Character editor module of INSTALL.PRG in the original):
/// 12 portraits, 17 buttons and the title, all with 4 bit planes, and the palette of the title.
/// </summary>
internal static class CharacterCreatorData
{
	public const int PortraitCount = 12;
	public const int ButtonCount = 17;
	const int PortraitSize = 32 * 34 / 2;
	const int ButtonSize = 32 * 16 / 2;
	const int TitleSize = 320 * 32 / 2;
	const int PaletteSize = 16 * 2;
	static readonly Lazy<byte[]> data = new(Load);

	static byte[] Load()
	{
		using var stream = typeof(CharacterCreatorData).Assembly.GetManifestResourceStream("CharacterCreator.dat")
			?? throw new InvalidOperationException("The graphics of the character editor are missing.");
		var bytes = new byte[stream.Length];
		stream.ReadExactly(bytes);
		return bytes;
	}

	public static byte[] GetPortraitData(int index) => data.Value[(index * PortraitSize)..((index + 1) * PortraitSize)];

	public static byte[] GetButtonData(int index)
	{
		int offset = PortraitCount * PortraitSize + index * ButtonSize;
		return data.Value[offset..(offset + ButtonSize)];
	}

	public static byte[] GetTitleData()
	{
		int offset = PortraitCount * PortraitSize + ButtonCount * ButtonSize;
		return data.Value[offset..(offset + TitleSize)];
	}

	public static byte[] GetTitlePaletteData()
	{
		int offset = PortraitCount * PortraitSize + ButtonCount * ButtonSize + TitleSize;
		return data.Value[offset..(offset + PaletteSize)];
	}

	/// <summary>
	/// Finds the portrait with the given graphic data.
	/// </summary>
	public static int? FindPortrait(ReadOnlySpan<byte> portraitData)
	{
		var portraits = data.Value.AsSpan(0, PortraitCount * PortraitSize);

		for (int i = 0; i < PortraitCount; i++)
		{
			if (portraits.Slice(i * PortraitSize, PortraitSize).SequenceEqual(portraitData))
				return i;
		}

		return null;
	}
}
