using Amberstar.Frontend;
using Amberstar.GameData;

namespace Amberstar.Tests;

public sealed class ItemHandsTests
{
	[Theory]
	[InlineData("German/AmberfilesAmiga")]
	[InlineData("German/AmberfilesST")]
	[InlineData("English/AmberfilesAmiga")]
	public void WeaponsShieldsAndAmmunitionNeedAHand(string folder)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder));

		var items = game.Game.AssetProvider.ItemLoader.LoadAllItems()
			.Where(item => item.Type is ItemType.CloseRangedWeapon or ItemType.LongRangedWeapon or ItemType.Shield or ItemType.Ammunition);

		Assert.NotEmpty(items);
		Assert.All(items, item => Assert.NotEqual(0, item.Hands));
	}

	[Theory]
	[InlineData("German/AmberfilesAmiga")]
	[InlineData("German/AmberfilesST")]
	[InlineData("German/AmberfilesDOS")]
	[InlineData("English/AmberfilesAmiga")]
	public void RangedWeaponsWithAmmunitionNeedOneHand(string folder)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder));

		// The LANGBOGEN DES SOBEK (193) needs two hands in the German data, so its arrows couldn't be equipped.
		var items = game.Game.AssetProvider.ItemLoader.LoadAllItems()
			.Where(item => item.Type == ItemType.LongRangedWeapon && item.UsedAmmoType != AmmoType.None).ToList();

		Assert.Contains(items, item => item.Index == 193);
		Assert.All(items, item => Assert.Equal(1, item.Hands));
	}

	[Theory]
	[InlineData("German/AmberfilesAmiga", "GOLDRING")]
	[InlineData("German/AmberfilesST", "GOLDRING")]
	[InlineData("English/AmberfilesAmiga", "GOLD RING")]
	public void TheItemListContainsAllItemsOfTheData(string folder, string goldRing)
	{
		using var game = new HeadlessGame(TestData.GetDataPath(folder));

		// Both items are not in a backpack or chest, only worn by someone or sold.
		Assert.Contains(game.Game.GetItems(), item => item.Index == 284 && item.Name == goldRing);
		Assert.Contains(game.Game.GetItems(), item => item.Index == 18);
	}
}
