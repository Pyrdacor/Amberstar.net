using Amber.Common;
using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game.UI;

/// <summary>
/// Shows the name of the item under the mouse (Show_object_name, Show2_object_name and
/// Upd_objname_print in the original). Outside of the inventory the original doesn't show
/// the names of monster items.
/// </summary>
internal sealed class ItemNameDisplay
{
	const int TextColorIndex = 15;
	/// <summary>
	/// Most screens show the name right of the picture (Upd_objname_print in the original).
	/// </summary>
	public static readonly Rect DefaultArea = new(112, 73, 192, 7);
	readonly Game game;
	readonly Label label;
	readonly bool showMonsterItems;

	/// <summary>
	/// Creates the display in the given area of the screen. Like all controls it should be created in Init.
	/// </summary>
	public ItemNameDisplay(Game game, Screen screen, Rect area, bool showMonsterItems = false, byte displayLayer = 100)
	{
		this.game = game;
		this.showMonsterItems = showMonsterItems;
		var (x, y, width, height) = area;
		label = screen.AddLabel(x, y, width, height, displayLayer);
		label.Alignment = TextAlignment.Center;
		label.Visible = false;
	}

	public bool Visible => label.Visible;

	/// <summary>
	/// Shows the name of the item under the mouse or hides it if there is none.
	/// </summary>
	public void Update(IEnumerable<ItemContainer> items, Position position, byte paletteIndex)
	{
		var item = items.FirstOrDefault(item => !item.Empty && !item.TwoHandedSecondSlot && item.Visible && item.Contains(position))?.Item;

		if (item == null || (!showMonsterItems && item.Type == ItemType.MonsterItem))
		{
			Hide();
			return;
		}

		label.SetText(item.GetName(game.AssetProvider.TextLoader), 320 - label.Position.X, TextColorIndex, 0, paletteIndex);
		label.Visible = true;
	}

	public void Hide() => label.Visible = false;
}
