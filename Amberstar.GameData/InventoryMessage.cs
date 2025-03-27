namespace Amberstar.GameData;

public enum InventoryMessage
{
	DropWhichItem,
	ItemCannotBeDropped,
	UseWhichItem,
    ExamineWhichItem,
	TransferWhichItem,
	NoMemberHasRoomForItem, // or can carry that much
	TransferHowManyGold,
	TransferHowManyFood,
    ItemNotEquippable,
	WrongClass,
	WrongGender,
	NotEnoughFreeHands,
	NotEnoughFreeFingers,
	NoRoomForItem,
    HowManyItemsToTransfer,
    HowManyItemsToDrop,
	FlyingDiscNotUsableHere,
    ItemIsCursed,
	ItemFulfillsSpecialPurposeNow,
    WhomToTransferTo,
	NotEquippableDuringFight,
	SpellIsNotUsableHere,
	ReallyDropItem,
	SameItemAlreadyInUse,
	ReallyDropGold,
	ReallyDropFood,
	LastMessage = ReallyDropFood
}
