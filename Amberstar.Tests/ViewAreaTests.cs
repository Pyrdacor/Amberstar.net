using Amberstar.Frontend;

namespace Amberstar.Tests;

public sealed class ViewAreaTests
{
	[Theory]
	[InlineData(1280, 800, 0, 0, 1280, 800)] // same aspect ratio
	[InlineData(1920, 1080, 96, 0, 1728, 1080)] // bars at the sides
	[InlineData(1000, 1000, 0, 187, 1000, 625)] // bars at the top and bottom
	public void TheAspectRatioIsKept(int windowWidth, int windowHeight, int x, int y, int width, int height)
	{
		Assert.Equal(new ViewArea(x, y, width, height), ViewArea.Calculate(windowWidth, windowHeight));
	}

	[Fact]
	public void MousePositionsRespectTheBars()
	{
		var area = ViewArea.Calculate(1920, 1080);

		Assert.Equal((0.0f, 0.0f), area.ToVirtual(96, 0));
		Assert.Equal((160.0f, 100.0f), area.ToVirtual(960, 540));
		// on the bars
		Assert.Equal((0.0f, 100.0f), area.ToVirtual(10, 540));
		Assert.Equal((319.0f, 100.0f), area.ToVirtual(1900, 540));
		Assert.Equal((960.0f, 540.0f), area.FromVirtual(160, 100));
	}
}
