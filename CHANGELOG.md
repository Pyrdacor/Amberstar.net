# Changelog

## Unreleased

### Changed
- The music list of the cheat mode is now opened with Ctrl+D, as the key D also moves the party.

### Fixed
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
