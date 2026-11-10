# Changelog

## Unreleased

### Added
- Cheat `noclip`: the party moves through walls and other obstacles on 2D and 3D maps, like the super chicken of the original.
- Cheats `members` and `invite`: show the characters who can join the party and add one of them by index or name, like asking to join but always successful regardless of the join chance (the person is removed from its map and is there again after the dismissal).

### Changed
- The party walks faster on 2D maps outside of the world map (about 10 steps per second instead of 4), like the original. There each step waited for about 4 to 6 vertical blanks.

### Fixed
- Bought items are shown on the counter of the merchant at once. Before they only appeared after the next action.
- DOS versions: the mouse pointer has its own colors (a silver sword with a black outline) instead of the colors of the UI palette, and the sword and shield next to the damage and protection values in the inventory are shown. They were not found in the DOS data before.
- The first step or turn right after entering a map is no longer swallowed. The map took its opening for a step and ignored the next one for about a quarter of a second.

## 0.4.2

### Added
- Enter switches between the movement and the action buttons on maps like the right click on the buttons. The original does this with Enter on the numeric keypad, which works too now.

### Fixed
- Holding the mouse button on Zzz (the middle movement button or the party on the map) or key 5 of the numeric keypad lets the time pass until it is released, like in the original. Before every step needed a click.
- Holding the mouse button on an arrow button moves the party like holding the key at the same place of the numeric keypad, like in the original. A click is still one step. Before every step needed a click.
- The whirlpool to the realm of Vielauge (world map part 3 at 46,11) can be entered with the ship. Map exits are only blocked for horses, rafts and ships where the party could get off, the deep water around the whirlpool only allows the ship. Like in the original the party continues on foot and the ship is lost.
- Encounter events (e.g. the final battle in the ritual room of Godsbane) start the battle at once and the monsters are only gone after the party won, like in the original program. After fleeing the party is back at its previous position and the monsters are still there. Before Amberstar.net asked to fight or flee and the monsters were gone after the party fled or avoided the battle, so the final battle could be skipped. The released source code works like that, but the shipped original program was changed.
- Shandra's rescue after a lost battle shows the picture of the magic prison and the game over shows the picture of the dead people next to the text, like in the original. Before only the text was shown.
- The icons of the active spells (light, armour protection, weapon power, anti-magic, clairvoyance and invisibility) are shown below the special items next to the map, like in the original. Before the area stayed empty.
- The name of the item under the mouse is shown in the inventory, opened chests, the loot after battles, conversations, at merchants and sages and in the camp while learning spells, like in the original. Before the name was only shown while choosing an item (e.g. to use or drop it). Like in the original the names of monster items are only shown in the inventory, and the name is shown right of the picture in chests and when items are used on locks.

## 0.4.1

### Fixed
- The DOS versions of the release crashed at the start. The game data archive of the executable only had the files of the Amiga and Atari ST versions, now it also has the files of the DOS versions.
- A spell scroll of a known spell can no longer be learned, like in the original. The selection ends without a message, the scroll and the spell learning points are kept. Before the spell learning points were used and the scroll crumbled to dust.
- The guilds train up to level 50 like the original game. The limit was 20 like in the released source code.

## 0.4.0

### Added
- The English and German game data of the DOS version (1.40) are supported and included. The music is played by an emulation of the Ad Lib sound card. The intro and the outro of the DOS version are not shown yet.

### Changed
- Ranged weapons which need ammunition always need one hand. The LANGBOGEN DES SOBEK of the German data needs two hands by mistake, so its arrows couldn't be equipped (the English data has one hand).
- The known words of a conversation are sorted alphabetically. They were in the order of the dictionary.

### Fixed
- The walls at the entrance of the temple of the Sansri open at 23:50. Persons with a path now take the positions of their path without a check of the tile, like in the original. The walls were stuck because their next position was blocked.
- The camp plays the lullaby like in the original, and the song of the map plays again when the camp is left.
- The spell of a used item (e.g. the torch, the flute or the rope) follows after the item sparkled, like in the original. Before the spell happened at once.
- The buttons of the inventory no longer stop working after the rope (or another item with levitation) was used to climb through a trap door in the ceiling. Until the game was restarted the inventory could only be closed with Escape, also after loading a savegame.
- Party members who would carry too much (or who can't take items for another reason) can no longer take an item from a chest or a person by a click on their portrait. Their portrait showed no hand, but the item was given anyway.
- Locked doors and chests show the picture of their state. After an open chest the next locked chest showed an open chest too. Like in the original, a chest without a save index is locked again when it is opened another time.
- The list of spells can no longer be scrolled outside of the window, and the last spells can be reached. A list started with the scroll position of the last time it was shown.
- Objects at the sides of the 3D view are cut at its border. Before they were drawn over the screen next to it, e.g. the walls of the temple of the Sansri.
- Escape no longer closes the map, which left an empty screen.
- A crash after a teleport (e.g. to the guild of the rangers) when hidden tiles of the last map were still animated with the tileset of the new map.

## 0.3.5

### Fixed
- Empty chests and items on the floor no longer open a screen and the party can walk over them on 3D maps, like in the original. The party was set back to the previous tile.
- Using the flute closes the inventory. The map was shown above it.
- Ranged weapons without an ammunition type, the sickle of return and the returning dart, can attack without ammunition.
- Party members who are stunned, asleep, mad, petrified or dead can no longer become the active member on the map, like in the original. In the camp this was already not possible.
- Two party members could stand on the same position in battles if someone was moved to the position of an empty party slot before a new member joined, so one of them was not available. This also repairs older savegames.

## 0.3.4

### Added
- A settings screen with its own button in the options: the resolution, window, fullscreen window or native fullscreen and the regeneration in the camp (10% like in the original or 50%). The settings are stored. In fullscreen the aspect ratio of the game is kept with black bars. F11 switches the window mode.

### Fixed
- Weapons, shields and ammunition which need no hand in the data (MÖRDERKLINGE and PARADESCHILD) now need one hand. This also fixes a crash if the PARADESCHILD was equipped together with a two-handed weapon.
- A short key press no longer delays the next step on 2D maps by a whole step. The party also walks a bit faster in cities and starts to walk continuously sooner.
- Transferring items from a chest stays active until a right click like in the original, so the button is not needed for every item.
- The buttons of doors and chests are updated if the active party member changes.
- The teleport cheat no longer crashes with a direction which does not exist.
- The console commands "items" and "item" also know the items which persons and monsters wear and merchants sell, for example the gold ring.
- The game no longer crashes when giving the bone to Firlas in the Amiga versions (a sharp s in runes) or with other characters which the font does not have.
- The hands at the portraits in places like merchants and libraries show who can take something, like in the original. A full backpack, dead party members and animals were not considered, and party members could also be given items they could not carry.
- A waiting raft or ship protects from swimming damage when stepping onto it, like in the original. A transport replaces the properties of its tile.
- The damage values at the portraits no longer stay visible if the party falls through a trap door.
- The map no longer appears above the screen of a riddlemouth if the right answer changes a tile.
- The spell selection always shows the sword cursor instead of the previous cursor, for example a movement arrow.
- The game no longer crashes when an item is dropped or given away while the last used item of its slot still sparkles. The buttons of the inventory wait for the end of the animation.
- The game no longer freezes if a monster casts a spell or shoots and nobody of the party is left on the battle field.

## 0.3.3

### Changed
- The spells to resurrect a person show a text if the person is ashes or dust or if they have no effect. The original shows nothing, and dead, ashes and dust look the same.

### Fixed
- Resting in the camp gives at least 1 hit point, like in the original.
- Resting in the camp or an inn also restores the hit points and spell points of bonuses from items.
- The items to choose from are shown again when an item is used on a locked chest.
- Negative damage or protection, for example from cursed items, are shown as 000 instead of ***.
- On 3D maps figures with a blocking graphic block the party, like in the original. Before the party could walk through them.
- On 3D maps the party can walk onto all tiles which are not blocked, like in the original. For example the skeletons in the tower of the white wizards can be touched now.
- The experience, hit points and spell points of a person are no longer shown in conversations, like in the original. The texts of spell casters were outside of the box.

### Added
- The console commands "items" to list all items and "item" to give an item by its index or name.

## 0.3.2

### Added
- Party members who carry too much are overloaded, like in the original. They show an icon at the portrait and the whole party can't move until the weight is reduced.

### Changed
- The music list of the cheat mode is now opened with Ctrl+D, as the key D also moves the party.

### Fixed
- The character screen shows the conditions of a party member again.
- The status icons at the portraits are updated at once when a condition changes, for example when a trap or poisonous ground poisons a party member.
- Languages which were learned in the game, like the language of animals from Sunny, are now kept in savegames.
- In battles the text, the damage and the hit points are now shown while the attack or spell animation is running, like in the original. Before they were shown after it.
- Hunting and staying monsters were swapped. For example the rat king in the sewers of Twinlake now waits in his room behind the scene at the altar, like in the original.
- The name, type and gender of an item were missing when an item was examined for the second time.
- The buttons of a door or chest were greyed out after another door or chest was opened, until it was shown a second time.
- Persons who only say a short text can now be talked to. Nothing happened before.
- The list of savegames in the options no longer shows all slots over the screen after the inventory or the dismiss window was closed.
- The dungeon map and the map overview no longer stay visible above the inventory or character screen which is opened while they are shown.
- Keys for one usage and lockpicks are now removed from the inventory when they are used at a door or chest. Before they only disappeared from the shown slots.
- The inventory and the character screen show the portrait of the character editor. The character screen also shows the current name and values of the party member instead of the original ones.
- The music list always shows the sword cursor instead of the previous cursor, for example a movement arrow.

## 0.3.1

### Added
- The cheat mode of the original: typing TEMPUS FUGIT activates it and the key D then opens the music list to play all songs.

### Changed
- Horses, rafts and ships can no longer enter map exits like the entrances of cities, so they are no longer lost there. The original also blocks the entrance of Twinlake with a horse.

### Fixed
- Two screens could open at the same time, for example the map and the options with the keypad keys 1 and 3, which showed a broken screen and crashed the game. Input is now ignored while a screen opens or closes.
- A crash when a character which the font can't show was typed into a text input, for example with Alt+number. Such characters are now ignored.
- Items of the loot after a battle could not be taken or examined once a chest was opened before. The same applies to using items on doors and chests.
- The digits of the keypad no longer select party members. Only the number keys 1 to 6 do this.
- Horses, rafts and ships can no longer pass tiles with events which do not allow them, like in the original.
- Transports are no longer hidden by the overlay of their tile, for example a horse on a path. Like in the original the transport replaces it.

## 0.3.0

### Added
- The character editor of the original installer is shown at the start of a new game. The name, the gender and the portrait can be changed and the values can be rolled.
- A main menu after the intro: continue the game, start a new game, load a game or quit.
- Up to 30 named savegames besides the savegame to continue the game. The options show them in a list to save or load, and the main menu can load them with the newest savegames first. Saving to a slot also updates the savegame to continue the game.
- The button with the party positions opens a screen to choose the positions of the party in battles.

### Changed
- Party members are no longer dismissed when saving. The options have their own button for it.

### Fixed
- A crash when another event started while a door or chest opened.
- A crash when the last party member died by swimming in the same step in which time passed.
- Sleeping in an inn no longer shows the map over the camp. The camp was opened before the inn was closed.
- The eye no longer starts a conversation with a person, so sleeping persons can no longer be spoken to this way. Like in the original only the mouth talks to persons.
- A person can no longer join the party several times. The keys of the numeric keypad still clicked the buttons of a conversation while a text was shown.

## 0.2.1

### Changed
- The compass also shows the direction of the party on 2D maps. In the original it always points north there.

### Fixed
- The keys of the numeric keypad match the positions of the buttons. The upper and lower row were swapped, so 1 used the eye instead of the map. When a movement key was released the party also made a move in the wrong direction.
- A screen shows its own layout again after the inventory was opened from it. In a conversation the layout of the inventory stayed visible.
- The battle screen of the Amiga versions is no longer black. Its colors and other data were not loaded from the Amiga program: the positions of the monsters, the damage of spells, the damage digits, the shadows on maps, the song names and the altar puzzle.
- The game no longer crashes when it is closed.
- The count of items no longer stays visible on the map after leaving a merchant.
- The "ouch" bubble no longer stays visible on the map when the party bumps into something several times in a row.
- Movement keys no longer close text popups, so a text which appears while walking stays until it is confirmed with the mouse, Space, Enter or Escape.
- Items can be used while the party stands on a map event. For example the magic picture of the family house only closed the inventory without any effect when it was used on the tile of its chest.

## 0.2.0

### Fixed
- Doors and chests no longer show their last message again when they are opened. This was the "the item opens the door" message at doors which were never opened.
- The right key always opens its door, also if the key is not used up.
- Fully locked doors and chests can no longer be picked, and the difficulty of a lock now counts. Failed attempts make a lock harder until another map is loaded, like in the original.
- Equipped items cover the placeholder of their slot (e.g. the hand).
- The party and other persons sit on chairs and lie in beds instead of disappearing below them.
- Persons can only be talked to if the active party member knows one of their languages.
- The amount windows no longer have text which is too long for the window. The long question is shown above the window.

### Added
- Sleeping persons only snore when they are spoken to, and talking to yourself shows a message.
- Gold and food can be thrown away in the amount window of the inventory.
- Holding the left mouse button on an arrow of an amount window changes the amount faster and faster.

## 0.1.0

First alpha version for Windows and Linux with the English and German game data of the Amiga and Atari ST versions included.
