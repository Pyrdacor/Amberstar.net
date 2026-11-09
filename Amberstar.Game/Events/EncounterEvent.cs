using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class EncounterEvent(IEncounterEvent @event, int eventIndex) : Event(@event, eventIndex), IEncounterEvent
	{
		public byte Chance => @event.Chance;

		public byte Quest => @event.Quest;

		public byte QuestTextIndex => @event.QuestTextIndex;

		public byte NoQuestTextIndex => @event.NoQuestTextIndex;

		public word MonsterGroupIndex => @event.MonsterGroupIndex;

		// the event is saved when the party wins the battle
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			if (!game.Probe(Chance))
				return false;

			if (Quest != 0 && !game.State.IsQuestBitSet(Quest))
				return false;

			byte textIndex = Quest != 0 ? QuestTextIndex : NoQuestTextIndex;
			// the map of the event (a part of the world map) is the current one only now
			int mapIndex = game.EventHandler.CurrentEventMapIndex;
			void StartBattle() => game.StartEncounter(MonsterGroupIndex, this, mapIndex);

			if (textIndex < TextEventExtensions.NoText)
				game.ShowTextMessage(game.GetMapText(game.EventHandler.CurrentEventMapIndex, textIndex), StartBattle);
			else
				StartBattle();

			return true;
		}
	}
}
