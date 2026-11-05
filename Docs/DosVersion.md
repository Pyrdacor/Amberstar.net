# DOS version (1.40, "IBM AT version" by ARTS)

The DOS version has the same game data as the Atari ST version, but stored differently. `DosGameData` loads it and
`DosConverter` converts each asset to the format of the Atari ST, so the same loaders read all versions.

## Archives (ICS/ARTS, `IcsArtsArchive`)
- Header: "ICS/ARTS", the entry count (word) and 21 bytes per entry: the file offset (dword), the method (byte), the name
  (12 characters, padded with spaces), the unpacked size and the packed size (words, only the low word of large entries).
- Methods 0 and 2 are stored. Method 1 is run length encoded (GAME.EXE $A96F): the data starts with $80, then
  1-127 repeats the next byte, 129-255 copies the count minus 128 bytes, 128 skips a byte and 0 ends the data.
- The German data of the published installer had 4 damaged disk tracks (MAP_DATA.026-028, SPRBLOCK.022-023, ROW001.019,
  ENDANI_1.PIX). They were repaired with the equal entries of the English data.

## Program (GAME.EXE)
- Turbo Pascal 7. The program starts with a call of the system unit, which loads the data segment (mov dx,segment).
- DS:058E altar puzzle (text indices, piece order, count, 12 x and 12 y positions, names as string[12]; no signs),
  DS:0664 song names (string[15]), DS:07AC "ICS/ARTS", DS:08CE UI texts (array of string[11]).
- Texts of GAME.EXE use the umlauts of code page 437 (like the Atari ST). The dictionary (DICTIONA.GER or .ENG) has the
  format of the Atari ST, but the characters are stored minus 32, and [, \, ], ^ are Ä, Ü, Ö, ß.
- The damage table of the spells, the positions of the monsters, the particle paths and the damage digits were not
  found. The values of the Atari ST version are used (`DosDefaults`).

## Data
- Words and longs are little endian. Strings are pascal strings.
- Arrays of records are often stored as one array per field: the 254 events of a map, the flags of the lab tiles
  (one array per byte, the lowest first) and of the tileset tiles, the class data and the altar puzzle.
- Maps: the lab tile arrays have 255 entries, the positions of the persons are 0-based.
- Characters (CHARDATA.VGA): 78 records of 2289 bytes without the first word of the Atari ST record and with an unused
  item before the 21 items. The texts of the conversations are in CODEDATA.CHA.
- Graphics have 4 bits per pixel, the left pixel in the high nibble. Rows of lab blocks have an even pixel count.
  The backgrounds have 1 byte per pixel (colors 16-31, the sky from color 160) and a second unused version of each graphic.
  The 80x80 pictures and the combat backgrounds have their VGA palette (48 bytes) in front of the pixels.
- Palettes have 6 bits per color component (COL_PALL.001-010 maps, 101 UI, 104 sky, 106 items, 107 automap, 110 puzzle).
- The font (FONT.DAT) has 96 glyphs of 6x5 pixels: the characters from 32 to 95 and 32 runes.
- The cursors (MAUSE.ICN) have colors, the windows (WINDOW.ICN) are made of 8 tiles.
- The loading screen (LOADING.ICN) has the dwarf of the Atari ST (128x112, 4 bits per pixel, the rest of the entry is
  unused) and uses COL_PALL.108, which has the colors of the Atari ST palette of the loading screen.
