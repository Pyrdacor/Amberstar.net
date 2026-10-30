using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class UIGraphicLoader(Amber.Assets.Common.IAssetProvider assetProvider, IGraphic emptyItemSlotGraphic) : IUIGraphicLoader
{
	private readonly Dictionary<StatusIcon, IGraphic> statusIcons = [];
	private readonly Dictionary<ButtonType, IGraphic> buttons = [];
	private readonly Dictionary<UIGraphic, IGraphic> uiGraphics = [];

	public IGraphic LoadStatusIcon(StatusIcon icon)
	{
		if (!statusIcons.TryGetValue(icon, out var gfx))
		{
			var asset = assetProvider.GetAsset(new(AssetType.StatusIcon, (int)icon));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Status icon {icon} not found.");

			gfx = Graphic.FromBitPlanes(16, 16, asset.GetReader().ReadBytes(16 * 16 / 2), 4);

			statusIcons.Add(icon, gfx);
		}

		return gfx;
	}

	public IGraphic LoadButtonGraphic(ButtonType buttonType)
	{
		if (!buttons.TryGetValue(buttonType, out var gfx))
		{
			if (buttonType > ButtonType.LastOriginalButton)
			{
                gfx = CreateCustomButton(buttonType);

                buttons.Add(buttonType, gfx);

                return gfx;
            }

			var asset = assetProvider.GetAsset(new(AssetType.Button, (int)buttonType));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Button {buttonType} not found.");

			gfx = Graphic.FromBitPlanes(32, 16, asset.GetReader().ReadBytes(32 * 16 / 2), 4);

			buttons.Add(buttonType, gfx);
		}

		return gfx;
	}

	private Graphic CreateCustomButton(ButtonType buttonType)
	{
		switch (buttonType)
		{
			case ButtonType.DistributeItems:
			{
				var distributeFoodButton = (LoadButtonGraphic(ButtonType.DistributeFood) as Graphic)!;
				var giveItemButton = (LoadButtonGraphic(ButtonType.GiveItem) as Graphic)!;

				// Prepare the base button
				var button = distributeFoodButton.FillRectsWithColor([new(12, 2, 8, 5), new(15, 7, 8, 7)], distributeFoodButton.GetColorIndexAt(2, 1));
				var itemImage = giveItemButton.GetPart(3, 4, 7, 7);
                button.AddOverlay(13, 3, itemImage, false);
                button.AddOverlay(11, 6, itemImage, false);

				return button;
			}
			case ButtonType.DismissPartyMembers:
			{
				// The parts of the button to ask someone to join swap their places: the group is left
				// and the person is right. They are not mirrored as the shadows are always on the right.
				const int Width = 32;
				const int Height = 16;
				var askToJoin = LoadButtonGraphic(ButtonType.AskToJoin).GetData();
				var pixels = (byte[])askToJoin.Clone();
				byte background = askToJoin[2 + 1 * Width];

				// the corners of the frame stay
				static bool IsFrameCorner(int x, int y) => (x == 30 && y == 1) || (x == 1 && y == 14) || (x >= 29 && y >= 13);

				for (int y = 1; y < Height - 1; y++)
				{
					for (int x = 1; x < Width - 1; x++)
					{
						if (!IsFrameCorner(x, y))
							pixels[x + y * Width] = background;
					}
				}

				void Move(int left, int top, int right, int bottom, int offsetX, Func<int, int, bool>? skip = null)
				{
					for (int y = top; y <= bottom; y++)
					{
						for (int x = left; x <= right; x++)
						{
							if (!IsFrameCorner(x, y) && skip?.Invoke(x, y) != true && askToJoin[x + y * Width] != background)
								pixels[x + offsetX + y * Width] = askToJoin[x + y * Width];
						}
					}
				}

				// The head of the arrow and the shadow of the group share a column.
				Move(18, 2, 29, 14, -17, (x, y) => x == 18 && y <= 9); // group
				Move(12, 3, 18, 9, 1); // arrow
				Move(1, 3, 11, 12, 19); // person

				return new Graphic(Width, Height, pixels, GraphicFormat.PaletteIndices);
			}
			default:
				throw new InvalidOperationException($"Button type '{buttonType}' is no valid custom button type.");
		}
	}

    public IGraphic LoadGraphic(UIGraphic graphic)
	{
		if (graphic == UIGraphic.EmptyItemSlot)
			return emptyItemSlotGraphic;

		if (!uiGraphics.TryGetValue(graphic, out var gfx))
		{
			var asset = assetProvider.GetAsset(new(AssetType.UIGraphic, (int)graphic));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"UI graphic {graphic} not found.");

			var size = graphic.GetSize();
			int frameCount = graphic.GetFrameCount();
			int width = (int)size.Width;
			int height = (int)size.Height;
			gfx = Graphic.FromBitPlanes(width, height, asset.GetReader().ReadBytes(frameCount * width * height / 2), 4, frameCount);

			uiGraphics.Add(graphic, gfx);
		}

		return gfx;
	}
}
