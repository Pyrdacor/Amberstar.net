using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class InventoryScreen : Screen
{
	Game? game;
    IPartyMember? partyMember;
    bool itemDragged = false;
    readonly Action draggingStartedHandler;
    readonly Action draggingEndedHandler;
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

    public InventoryScreen()
    {
        draggingStartedHandler = () =>
        {
            itemDragged = true;
            game!.Cursor.Visible = false;
        };
        draggingEndedHandler = () =>
        {
            itemDragged = false;
            game!.Cursor.Visible = true;
        };
    }

    public override ScreenType Type { get; } = ScreenType.Inventory;



    private void SetupEventHandlers()
    {
        ItemContainer.DraggingStarted += draggingStartedHandler;
        ItemContainer.DraggingEnded += draggingEndedHandler;
    }

    private void CleanUpEventHandlers()
    {
        ItemContainer.DraggingStarted -= draggingStartedHandler;
        ItemContainer.DraggingEnded -= draggingEndedHandler;
    }

    public override void Init(Game game)
    {
        base.Init(game);

        this.game = game;

        for (int i = 0; i < inventoryItemSlots.Length; i++)
            inventoryItemSlots[i] = new ItemContainer(game, InventorySlotPositions[i], 0, null, 10) { Draggable = true };

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
            equippedItemSlots.Add(equipmentSlot, new ItemContainer(game, EquipmentSlotPositions[equipmentSlot], 0, null, 10) { Draggable = true });
    }

    public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

        var palette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        game.SetLayout(Layout.Inventory, palette);
        game.Cursor.CursorType = CursorType.Sword;

        SwitchToPartyMember(game.State.CurrentInventoryIndex!.Value, true);

        SetupEventHandlers();
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
        CleanUpEventHandlers();

        CleanUpItems();

        base.Close(game);
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        CleanUpEventHandlers();

        base.ScreenPushed(game, screen);
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        SetupEventHandlers();
    }

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		game!.ScreenHandler.PopScreen();
	}

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (itemDragged && buttons == MouseButtons.Right)
        {
            ItemContainer.AbortDrag();
            return;
        }

        foreach (var inventorySlot in inventoryItemSlots)
        {
            if (inventorySlot.MouseClick(position, buttons))
                return;
        }

        foreach (var equippedItemSlot in equippedItemSlots)
        {
            if (equippedItemSlot.Value.MouseClick(position, buttons))
                return;
        }
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        base.MouseMove(position, buttons);

        ItemContainer.UpdateDragPosition(game!, position);
    }

    public void SwitchToPartyMember(int index, bool force)
    {
        if (!force && game!.State.CurrentInventoryIndex == index)
            return;

        CleanUpItems();

        game!.State.CurrentInventoryIndex = index;
        int partyMemberIndex = 1; // TODO: get from savegame, slot is index
        partyMember = (game!.AssetProvider.PersonLoader.LoadPerson(partyMemberIndex) as IPartyMember)!;
        var graphicLoader = game!.AssetProvider.GraphicLoader;
        var uiPaletteIndex = game!.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

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
