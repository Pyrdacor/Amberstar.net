using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game.UI;

internal class ItemContainer
{
	public const int Width = 16;
	public const int Height = 16;
	public const int TwoHandedSecondSlotMarker = -1;
	readonly Game game;
	Position position;
    ISprite? sprite;
	ISprite? brokenOverlay; // TODO
	// TODO: Item count display
	byte paletteIndex;
	byte displayLayer;

	static Position? draggedPosition;
	static ItemContainer? draggedSourceSlot;
    static ItemContainer? DraggedItem { get; set; }

    public event Action<ItemContainer>? Dragged;
    public event Action<ItemContainer>? Dropped;

    public byte PaletteIndex
	{
		get => paletteIndex;
		set
		{
			paletteIndex = value;

			if (sprite != null)
				sprite.PaletteIndex = value;
			if (brokenOverlay != null)
                brokenOverlay.PaletteIndex = value;
		}
	}

	public bool Empty => ItemCount == 0 || Item == null;

	public bool TwoHandedSecondSlot => ItemCount == TwoHandedSecondSlotMarker && Item != null;

    public bool Broken
	{
		get => brokenOverlay?.Visible ?? false;
		set
		{
			if (value && ItemCount <= 0)
                return;

            if (brokenOverlay != null)
				brokenOverlay.Visible = value;
			else if (value)
                CreateBrokenOverlay();
        }
	}

	public bool Draggable { get; set; }

	public Position Position
	{
		get => position;
		private set
		{
			if (position == value)
                return;

			position = value;

			UpdateRenderPosition(position);
        }
    }

	public int ItemCount { get; private set; }

	public IItem? Item { get; private set; }

    public ItemContainer(Game game, Position position, int itemCount, IItem? item, byte displayLayer)
	{
		this.game = game;
		this.position = position;
		this.paletteIndex = game.PaletteIndexProvider.ItemPaletteIndex;

        if (displayLayer > byte.MaxValue - 2)
            displayLayer = byte.MaxValue - 2;

		this.displayLayer = displayLayer;

        if (itemCount > 0 && item == null)
			throw new AmberException(ExceptionScope.Application, "Item count is greater than 0 but item is null.");
		if (itemCount <= 0 && item != null)
            throw new AmberException(ExceptionScope.Application, "Item count is less than 1 but item is not null.");

        if (item != null)
			SetItem(itemCount, item);
	}

	private void CreateBrokenOverlay()
    {
		// TODO

        /*var layer = game.GetRenderLayer(Layer.UI);
        var textureAtlas = layer.Config.Texture!;

        brokenOverlay = layer.SpriteFactory!.Create();
        brokenOverlay.Position = Position;
        brokenOverlay.Size = new(Width, Height);
		// TODO
        //brokenOverlay.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.BrokenIcon));
        brokenOverlay.DisplayLayer = (byte)(sprite.DisplayLayer + 1);
        brokenOverlay.PaletteIndex = paletteIndex;
        brokenOverlay.Visible = true;*/
    }

    /// <summary>
    /// Tries to drop the given items.
    /// 
    /// Note: This will not exchange items, only drop them.
	/// So if there is already an item, and the given item
	/// cannot be dropped, it will return the given count.
    /// 
    /// Returns the amount of remaining dragged items.
    /// </summary>
    public int DropItem(int count, IItem item)
	{
		if (ItemCount == 0)
		{
			SetItem(count, item);
			return 0;
		}
		else if (ItemCount == TwoHandedSecondSlotMarker)
		{
			// TODO? Drop on second two-handed slot.
			return count;
		}
		else if (Item!.Index == item.Index)
		{
			if (!Item.IsStackable())
				return count;

			int dropCount = Math.Min(count, 99 - ItemCount);

			SetItem(ItemCount + dropCount, Item);

			count -= dropCount;

			return count;
		}
		else
		{
			// Item is different, cannot drop.
			return count;
		}
	}

	public bool ExchangeItem(int count, IItem item)
	{
		// TODO
		return false;
	}

    public void SetItem(int count, IItem item)
	{
        if (count > 0 && item == null)
            throw new AmberException(ExceptionScope.Application, "Item count is greater than 0 but item is null.");
        if (count <= 0 && item != null)
            throw new AmberException(ExceptionScope.Application, "Item count is less than 1 but item is not null.");

        ItemCount = count;
        Item = item;

        if (count == 0)
		{
			ClearItem();
		}
		else
		{
            var layer = game.GetRenderLayer(Layer.UI);
            var textureAtlas = layer.Config.Texture!;

            var itemGraphicIndex = game!.GraphicIndexProvider.GetItemGraphicIndex(item!.GraphicIndex);

			if (sprite == null)
			{
				sprite = layer.SpriteFactory!.Create();
				sprite.Position = Position;
				sprite.Size = new(Width, Height);
                sprite.DisplayLayer = displayLayer;
                sprite.PaletteIndex = paletteIndex;
            }

            sprite.TextureOffset = textureAtlas.GetOffset(itemGraphicIndex);            
            sprite.Visible = true;

            // TODO: item count display, broken overlay
        }
    }

	public void ClearItem()
	{
		if (sprite != null)
        {
            sprite.Visible = false;
            sprite = null;
        }

		if (brokenOverlay != null)
		{
            brokenOverlay.Visible = false;
            brokenOverlay = null;
        }

		// TODO: item count display
    }

	public static void UpdateDragPosition(Position position)
	{
		if (DraggedItem == null)
			return;

		draggedPosition = position;

		DraggedItem.UpdateRenderPosition(position);
    }

    public static void AbortDrag()
    {
        if (DraggedItem == null)
            return;

        DraggedItem.UpdateRenderPosition(DraggedItem.Position);
        DraggedItem.Dropped?.Invoke(DraggedItem);
        DraggedItem = null;
		draggedSourceSlot = null;
    }

    public static void ConsumeDragged()
    {
        if (DraggedItem == null)
            return;

        DraggedItem.UpdateRenderPosition(DraggedItem.Position);
		DraggedItem.ClearItem();
        DraggedItem = null;
    }

    private void UpdateRenderPosition(Position position)
	{
		if (sprite != null)
			sprite.Position = position;

        if (brokenOverlay != null)
            brokenOverlay.Position = position;

        // TODO: item count display
    }

    public bool MouseClick(Position position, MouseButtons mouseButtons)
	{
        var upperLeft = Position;
        var lowerRight = new Position(upperLeft.X + Width, upperLeft.Y + Height);

        if (position.X < upperLeft.X || position.Y < upperLeft.Y || position.X >= lowerRight.X || position.Y >= lowerRight.Y)
            return false; // Not hit

        if (DraggedItem == null && ItemCount <= 0)
			return false; // No drop and no item in slot

		if (DraggedItem == null)
		{
			if (!Draggable)
				return false; // Can't pick up

			// TODO: if right mouse button, take all, else only one or show amount box

			// Drag the item
			DraggedItem = this;
            DraggedItem.Dragged?.Invoke(this);

            UpdateDragPosition(position);

			return true;
		}
		else
		{
			// Try to drop
			int previousCount = DraggedItem.ItemCount;
            DraggedItem.ItemCount = DropItem(DraggedItem.ItemCount, DraggedItem.Item!);

			if (DraggedItem.ItemCount == 0)
			{
				// Fully dropped
                DraggedItem.ClearItem();
                DraggedItem = null;
				return true;
            }

            if (previousCount != DraggedItem.ItemCount)
			{
				// Some were dropped
				return true; // Nothing else to do
            }

            // Try to exchange item
			if (ItemCount == TwoHandedSecondSlotMarker)
			{
                // TODO: Replace the main slot!
                return false;
            }
			else
			{
				return ExchangeItem(DraggedItem.ItemCount, DraggedItem.Item!);
			}
		}
	}

	public void Destroy()
	{
		ClearItem();
	}
}
