using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal class InventoryScreen : Screen
{
	Game? game;
    IPartyMember? partyMember;
    readonly ItemContainer[] inventoryItemSlots = new ItemContainer[ICharacter.InventorySlotCount];
    readonly Dictionary<EquipmentSlot, ItemContainer> equippedItemSlots = [];
    readonly static Dictionary<EquipmentSlot, Position> EquipmentSlotPositions = [];
    readonly static Position[] InventorySlotPositions = new Position[ICharacter.InventorySlotCount];

    static InventoryScreen()
    {
        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            int column = (int)equipmentSlot % 3;
            int row = (int)equipmentSlot / 3;
            var position = new Position(16 + column * 32, 37 + 44 + row * 32);

            EquipmentSlotPositions.Add(equipmentSlot, position);
        }

        for (int i = 0; i < InventorySlotPositions.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            var position = new Position(112 + column * 32, 37 + 44 + row * 32);

            InventorySlotPositions[i] = position;
        }
    }

    public override ScreenType Type { get; } = ScreenType.Inventory;

    public override void Init(Game game)
    {
        base.Init(game);

        this.game = game;

        for (int i = 0; i < inventoryItemSlots.Length; i++)
            inventoryItemSlots[i] = new ItemContainer(game, InventorySlotPositions[i], 0, null, 10);

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
            equippedItemSlots.Add(equipmentSlot, new ItemContainer(game, EquipmentSlotPositions[equipmentSlot], 0, null, 10));
    }

    public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

        var palette = game.PaletteIndexProvider.UIPaletteIndex;

        game.SetLayout(Layout.Inventory, palette);

        SwitchToPartyMember(game.State.CurrentInventoryIndex!.Value, true);
    }

    private void CleanUpItems()
    {
        foreach (var equippedItemGraphic in equippedItemSlots)
            equippedItemGraphic.Value.ClearItem();

        for (int i = 0; i < inventoryItemSlots.Length; i++)
            inventoryItemSlots[i].ClearItem();
    }

    public override void Close(Game game)
    {
        CleanUpItems();

        base.Close(game);
    }

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		game!.ScreenHandler.PopScreen();
	}

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        game!.ScreenHandler.PopScreen();
    }

    public void SwitchToPartyMember(int index, bool force)
    {
        if (!force && game!.State.CurrentInventoryIndex == index)
            return;

        CleanUpItems();

        game!.State.CurrentInventoryIndex = index;
        int partyMemberIndex = 1; // TODO: get from savegame, slot is index
        partyMember = game!.AssetProvider.PartyMemberLoader.LoadPartyMember(partyMemberIndex);
        var graphicLoader = game!.AssetProvider.GraphicLoader;
        var uiPaletteIndex = game!.PaletteIndexProvider.UIPaletteIndex;

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            if (partyMember.Equipment.TryGetValue(equipmentSlot, out var equipment))
            {
                if (equipment.Item != null && equipment.Count > 0)
                    equippedItemSlots[equipmentSlot].SetItem(equipment.Count, equipment.Item);
            }
        }

        // Check for two-handed weapon
        if (partyMember.Equipment.TryGetValue(EquipmentSlot.RightHand, out var rightHandEquipment) && rightHandEquipment.Item != null
            && rightHandEquipment.Item.IsTwoHanded())
        {
            if (equippedItemSlots[EquipmentSlot.LeftHand].ItemCount > 0)
                throw new AmberException(ExceptionScope.Application, "Two-handed weapon equipped but left hand is not empty.");

            equippedItemSlots[EquipmentSlot.LeftHand].SetItem(ItemContainer.TwoHandedSecondSlotMarker, rightHandEquipment.Item);
        }

        for (int i = 0; i < partyMember.Inventory.Length; i++)
        {
            var itemSlot = partyMember.Inventory[i];

            if (itemSlot?.Item == null || itemSlot.Count <= 0)
                continue;

            inventoryItemSlots[i].SetItem(itemSlot.Count, itemSlot.Item);
        }
    }
}
