# Amberstar intro (THALION.68K and INTRO.68K)

The intro programs are not part of the released source code. They were disassembled.

## Start
- The loader AMBRSTAR.PRG starts THALION.68K (the logo of Thalion), then INTRO.68K (not on "abnormal" STs),
  then the game AMBRSTAR.68K. When the game returns $2505, EXTRO.68K is started.
- INTRO.68K checks its command line ("Karsten ist doof !", like the outro). The data segment is LOB packed: the
  unpacked program has the code (0-$822), the data and the relocation table at the end. The addresses are from 0
  and the code is the same in all versions, so the data addresses are read from the operands (see `IntroData`).

## Logo of Thalion (THALION.68K)
- A PRG (not packed), the same in all versions. It draws every frame into the screen memory, so `ThalionLogo` ports
  the routines 1:1 on a copy of the memory (the program at address 0, the screens at $d400, $15100 and $1ce00).
  The main program is an iterator which waits for the vertical blanks, the VBL routine ($6a6) plays the music and
  calls the routines of the phases (L69e and L6a2). The shown frame is the screen of the last vertical blank.
- Start: the screen is filled with color 2, random drops are drawn on it (L2e6, the blocks below a drop are marked),
  the graphics are shifted for all positions (drops: 4 shifts, pieces of the logo: 16 shifts), the palette fades in
  ($11c8: every 4 vertical blanks each channel + 1). Song 1.
- Rain ($c52): up to 11 drops fall in their columns (a map of 20 bytes per line reserves the 34 lines of a drop).
  They slow down before they reach their line (table $eb2) and stay in both screens. When a new drop finds no space,
  no more drops start and the phase ends after the last one landed.
- Pieces ($9f0, song 6): 4 pieces of the logo (one plane each) move 1 pixel per vertical blank to their positions
  (table $a9c). They are or'ed into plane 3 (the rain uses the planes 0-2), so they look transparent (colors 8-15).
- Logo (song 8): the colors 8-15 fade to white, the logo (240x168 at 32,16) is drawn where the pieces are, the colors
  fade to the logo. The colors 1-4 of the drops become 10, 11, 13 and 14 (L862).
- Name (song 2): the name (192x51, 2 planes) bounces in (table $418e, then the loop at $4210). The VBL routine ($b82)
  starts Timer B ($c0a) which sets the colors 2 and 3 of each line of the name (51 colors and their dimmed versions).
  Then the space key ends it, the secret words AAARRGH (song 4) and SCHNISM (another bounce loop) can be typed.
  Before the end any key or the fire button ends it.
- End ($12d2): the palette and the colors of the raster fade to black.

## Intro (INTRO.68K)
- The title picture ($87c, 32000 bytes) is followed by the palettes of its 199 lines (3 x 16 colors per line).
  The VBL routine ($57c or $68e for 60 Hz) synchronises with the video counter and writes the palette 3 times
  in each line (like Spectrum 512). The color of a pixel is one of the 48 colors of its line, depending on its
  color and x position: x1 = 10 * color + (odd: -5, even: +1), the first palette before x1, the second until
  x1 + 160, then the third. The first line is black.
- 6 darker copies of the palettes are made (each channel decreased by 1 per copy). The title fades in over 24 vertical
  blanks (a step every 4) and waits for the space key, then it fades out the same way.
- The background ($d09c, a NEOchrome picture with its palette at $d020) fades in from black (7 steps of 3 vertical
  blanks, each channel is increased by 1 per step). The music starts (a COSO module with samples, played by the
  same emulation of the Amiga voices as in the outro). The logo ($14d9c, 320x36, masked) is drawn line by line at
  line 17 (one line per vertical blank).
- The credits ($4a1ba): pages which end with 0, lines end with 13, the list ends with an empty page. A page is drawn
  into the background with the font ($1641c, 16x20 pixels per character from $25, masked) at (32,75) with 22 pixels
  per line. Then a transition shows it (chosen by a random number, bit 5): 5 bands of 22 lines which show one more
  line per vertical blank, or rows of 4 lines where the upper 2 lines are shown from the left and the lower 2 lines
  from the right (16 pixels per vertical blank). Each page stays for 251 vertical blanks, then the next one follows
  (endless). The space key ends the intro: the palette fades out (7 steps of 3 vertical blanks).

## Amiga (the loader "Amberstar", TH_LOGO.UDO, INTRO.UDO and INTRO_P.UDO)
- The loader (the program "Amberstar" on disk A, "Amiga conversion by Udo Fischer") shows the logo of Thalion and the
  title picture itself, then it starts INTRO_P.UDO (the credits), shows the logo again while it loads AMBERDEV.UDO and
  starts the game. All .UDO files are LOB packed.
- TH_LOGO.UDO: a picture (320x200, 5 bit planes, the planes of each line after another). The palette is in the loader
  (L2ea, 32 colors). It is shown without fading until a key or the left mouse button is pressed (La48). The escape key
  (raw key code $45) skips the title picture and the credits.
- INTRO.UDO: 200 palettes of 16 colors (3 bits per channel, the loader doubles them), then the picture (5 bit planes
  of 8000 bytes). The copper list (L948) sets the colors 0-15 from the first palette before the picture and the
  colors 16-31 from the next palette at the end of each line, so line y uses the palette y (line 0 the last one).
  It is shown without fading until a key or the mouse button is pressed.
- INTRO_P.UDO: the same as INTRO.68K, rewritten for the Amiga (a PRG, the addresses are from 0):
  - The background ($30d4, 4 bit planes of each line after another) fades in from black with its palette ($688,
    4 bits per channel): 15 steps of 2 vertical blanks (L602).
  - The logo ($794, 320x33, each line of a plane is followed by its mask) scrolls up from the bottom to line 20,
    2 lines per vertical blank (double buffered, L440 restores the background and L3c4 draws it with the blitter).
  - Then the music starts (the player of the Amiga outro, "Player by Jochen Hippel 1990", with a TFMX module,
    song 1).
  - The credits ($3a9b8) are drawn with the font ($add4) like on the Atari ST (L354). The transitions are the same,
    but the rows show 8 pixels per vertical blank (40 steps). The lower 2 lines of the rows start at the second last
    column (an off-by-one error of the original), so their last 8 pixels keep the page before. The port shows them.
  - Each page stays for 251 vertical blanks. A key or the mouse button ends it: the palette fades out (15 steps of
    2 vertical blanks, L65a) and the music stops.

## Port
- `IntroData` loads the data (the title pictures as RGBA for each fading step, the pages with their text),
  `IntroScreen` shows it. `ThalionLogoScreen` shows the frames of `ThalionLogo` (a texture which is updated). The intro and the outro use their own layer with their own palettes.
- On the Amiga `IntroData` has one title picture and the timing of INTRO_P, `AmigaThalionLogo` is the picture of the logo.
- The intro is not shown for scenario scripts.
