using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;

namespace Amberstar.Tests;

public class DrowningTests
{
	/// <summary>
	/// A step lets time pass and the handlers of the time run in the next frame. If the last party
	/// member drowns in this step, the map is closed before them. It depends on random numbers,
	/// so several seeds are used.
	/// </summary>
	[Fact]
	public void TheLastPartyMemberCanDrownWhileTimePasses()
	{
		var path = TestData.GetDataPath("German/AmberfilesST");
		var moves = string.Concat(Enumerable.Range(0, 40).Select(_ => "move right 7 optional\nmove left 7 optional\n"));
		var text = $"waittext\ndismissall\njoin 61\nactivate 2\nteleport 37 6 6\nwait 2\ndismissall\nsetvalue 1 hp 1\nsetvalue 2 hp 1\n{moves}";

		for (int seed = 0; seed < 40; seed++)
		{
			var lines = new List<string>();
			using var game = new HeadlessGame(path, seed);
			game.RunScript(new ScenarioScript(text.Split('\n'), lines.Add), 120, lines.Add);

			Assert.DoesNotContain(lines, line => line.Contains("crashed"));
		}
	}
}
