using Amberstar.Game;

namespace Amberstar.Tests;

public sealed class ResolutionTests
{
	[Theory]
	[InlineData(1920, 1080, 640, 960, 1280)]
	[InlineData(1920, 1200, 960, 1280, 1600)]
	[InlineData(2560, 1440, 1280, 1600, 1920)]
	[InlineData(3840, 2160, 2560, 2880, 3200)]
	public void TheLargestWindowsWhichFitOnTheMonitorAreUsed(int monitorWidth, int monitorHeight, params int[] widths)
	{
		var resolutions = Resolution.ForMonitor(monitorWidth, monitorHeight);

		Assert.Equal(widths, resolutions.Select(resolution => resolution.Width));
		Assert.All(resolutions, resolution => Assert.Equal(resolution.Width * 5, resolution.Height * 8));
		Assert.All(resolutions, resolution => Assert.True(resolution.Width < monitorWidth && resolution.Height < monitorHeight));
	}

	[Theory]
	[InlineData(800, 600, 2)]
	[InlineData(320, 200, 1)]
	public void SmallMonitorsHaveLessWindowSizes(int monitorWidth, int monitorHeight, int count)
	{
		Assert.Equal(count, Resolution.ForMonitor(monitorWidth, monitorHeight).Length);
	}
}
