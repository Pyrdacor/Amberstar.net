using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
    internal class ChestEvent(IChestEvent @event, int eventIndex) : Event(@event, eventIndex), IChestEvent
    {
        public byte LockpickReduction => @event.LockpickReduction;

        public TrapType TrapType => @event.TrapType;

        public byte TrapDamage => @event.TrapDamage;

        public word TextIndex => @event.TextIndex;

        public bool Hidden => @event.Hidden;

        public word ChestIndex => @event.ChestIndex;

        // We need to save it only if the chest was opened!
        public override bool AutoSave => false;        

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            if (trigger != EventTrigger.Move && trigger != EventTrigger.Eye)
                return false;

            // Nothing happens at empty chests and the party stands on them (Item_found in the original).
            if (!game.CanOpenChest(this))
                return false;

            // The party can't stand on a chest which is shown in a 3D map.
            if (trigger == EventTrigger.Move)
                (game.ScreenHandler.FindMapScreen() as Screens.Map3DScreen)?.ResetPartyPosition();

            game.ShowChest();

            return true;
		}
	}
}
