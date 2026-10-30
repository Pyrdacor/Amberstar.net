# Changelog

## Unreleased

### Fixed
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
