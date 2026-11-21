# Translations

The texts of the game can be translated into other languages with [Amberstar.Translator](../Amberstar.Translator).
It writes all texts of the English (or German) game data to a [gettext PO file](https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html)
for the translators and creates a new game data folder with the translation.

The original data files are not changed. The translation is the file `TRANSLAT.PO` in the game data folder and the
game replaces the texts while it loads them. Texts which are not translated stay in the language of the data.

## Workflow

1. Create the template with all texts (English Amiga, Atari ST and DOS data of the repository, the German texts are
   added as comments for the translators):

   ```
   dotnet run --project Amberstar.Translator -- extract --output amberstar.pot
   ```

2. Give `amberstar.pot` to the translators. They save it as e.g. `fr.po` and fill in the translations (with a PO editor
   like [Poedit](https://poedit.net), a translation platform like Weblate or Crowdin, or a text editor).

3. Check the translation. It shows the errors, the warnings and how much is translated:

   ```
   dotnet run --project Amberstar.Translator -- check English/AmberfilesAmiga fr.po
   ```

   If it reports characters without glyph (e.g. é or Ł), create the glyphs for them (see [Glyphs](#glyphs)) and
   check again with `--glyphs glyphs.txt`.

4. Create the game data of the new language (a copy of the English data with the translation and the glyphs):

   ```
   dotnet run --project Amberstar.Translator -- create English/AmberfilesAmiga fr.po French/AmberfilesAmiga --glyphs glyphs.txt
   ```

   The game can be started with it (`Amberstar.net --data French/AmberfilesAmiga`) and it can be packed for the
   releases with the other versions:

   ```
   dotnet run --project Amberstar.VersionPacker -- pack versions.dat English-Amiga English Amiga English/AmberfilesAmiga French-Amiga Français Amiga French/AmberfilesAmiga ...
   ```

   The same translation works for the Amiga, Atari ST and DOS data (create a folder for each).

5. If the texts of the template change later (e.g. a new version of the tool), the translation can be updated. The
   translations of changed texts are marked as fuzzy (they are not used until a translator checked them):

   ```
   dotnet run --project Amberstar.Translator -- update fr.po amberstar.pot
   ```

   `msgmerge` of gettext works as well.

## Translations of Ambermoon

Ambermoon has French, Polish and Czech translations. Many texts are the same as in Amberstar (the names of the
classes, races, attributes, skills, conditions, item types, spells and items, some UI texts and messages). The
command `ambermoon` fills the untranslated texts of a template or translation with them. The source are the text
exports of the Ambermoon translators (`Disks/Bugfixing/<Language>/AllTexts` of the
[Ambermoon repository](https://github.com/Pyrdacor/Ambermoon)), the texts are found by the English text:

```
dotnet run --project Amberstar.Translator -- ambermoon amberstar.pot Ambermoon/Disks/Bugfixing/English/AllTexts Ambermoon/Disks/Bugfixing/French/AllTexts --german Ambermoon/Disks/Bugfixing/German/AllTexts --language fr --output fr.po
```

The imported texts are marked as fuzzy: the game ignores them until a translator checked them and removed the flag.
Each one has a comment with the file of Ambermoon. A text is only taken from the same kind of text (e.g. a condition
name of Amberstar from the condition names of Ambermoon). With `--german` the German texts of Ambermoon and Amberstar
are compared: if they differ, the comment says so, because the English word could mean something else (e.g. STONED
is "berauscht" in Ambermoon but "versteinert" in Amberstar). About 130 texts are found for each language. The French
texts of Ambermoon have no accents and some class names are abbreviated (the Amiga version had no room for more).

## Notes for the translators

Each entry has a key (`msgctxt`), the English text (`msgid`) and the translation (`msgstr`). The comments say where
the text is used, the maximum length and the German text. Empty translations keep the English text.

| Key | Texts | Notes |
| --- | --- | --- |
| `word` | Single words of the dictionary: names of items, spells, classes, races, skills, ..., conversation keywords and answers of riddle mouths | The translation replaces the word everywhere. Keywords are typed by the player, so they should be single words and each must be unique. Item names should be about as long as the English ones (the longest has 20 characters). |
| `maptext/…`, `conversation/…`, `message/…`, `itemtext/…`, `puzzletext/…` | The texts of the maps, the conversations, the messages, the item texts (letters, books) and the altar puzzle | The game wraps the lines itself. A line break is a new line (empty lines are kept) and `{P}` is a new paragraph. Words longer than 25 characters can't be wrapped. |
| `name/character`, `name/map`, `name/place` | Names of persons, monsters, maps and places | Max. 15 characters (persons and monsters) or 30 characters (maps and places). |
| `ui/…` | Texts of the user interface | Keep the codes like `{01}{08}` (colors) and the length: the spaces are used for the layout. |
| `outro/…` | The texts of the outro | The lines must not be longer than the English ones. |
| `program/…` | Texts of Amberstar.net (main menu, settings, messages) | Keep the placeholders like `{0}` (e.g. the name of a party member). |

Characters: the fonts of the game only have the uppercase letters A-Z (the lowercase letters are shown as uppercase
letters), the digits, some punctuation and the German special characters Ä, Ö, Ü and ß
(``$ & < > @ [ \ ] ^ ` { | } ~`` are missing). Other characters need new glyphs (see below).

## Glyphs

The glyphs of the characters which the fonts don't have are defined in a glyph file (`GLYPHS.TXT` in the game data
folder). The command `glyphs` finds the characters of a translation without glyph and writes drafts for them:

```
dotnet run --project Amberstar.Translator -- glyphs English/AmberfilesAmiga fr.po --output glyphs.txt
```

`--characters` adds further characters, e.g. all special characters of the language, so the glyph file is complete
before the translation is (French: `--characters "àâæçèéêëîïôœùûüÿ«»’"`).

Check and improve the drafts with a text editor. A glyph has 5 rows of 5 pixels (`#` is set, `.` is not set), like
the glyphs of the original. The special characters have the letter in the lower 4 rows and the mark in the first
row like the Ä of the original. A character can also show the glyph of another character (e.g. Cyrillic letters
which look like Latin letters):

```
# Ä of the original
Ä
##.##
.###.
##.##
#####
##.##

# draft of É (E with acute)
É
..##.
#####
##...
###..
#####

А = A
```

The drafts are made from the letters of the font and the marks (accents, cedilla, caron, ...), some letters (e.g. Ł,
Ø, Æ, Œ and the Cyrillic letters) are drawn and letters which look like Latin letters use their glyphs. Characters
without a draft get an empty glyph which `check` reports. Running `glyphs` again with `--glyphs glyphs.txt` adds the
characters which are still missing. Lowercase letters use the glyph of the uppercase letter.

The glyphs are used for the texts, the names, the user interface, the typed words and the outro. The fonts map each
character (byte) to a glyph: the characters of the Atari ST character set (like é) get their glyph at their place,
the other characters get a free place of the font (55 are free), so they can also be stored in the names of
the game data.

## How it works

The original stores the texts as lists of word indices of a dictionary (the program data or `DICTIONA.*` on DOS).
The translated texts are split into words when the game data is loaded. New words are added to the end of the
dictionary. The single words (`word`) replace the words of the dictionary, so the word indices of the items,
conversations, riddle mouths and savegames stay the same. The names, UI texts and the outro texts are replaced in the
data. The keys are the same for all versions, so one translation works for the Amiga, Atari ST and DOS data. Texts
which differ between the versions are found by their source text.

The code is in [Amberstar.GameData.Legacy/Translation](../Amberstar.GameData.Legacy/Translation) (extraction, PO
files and loading the translation) and [Amberstar.Translator](../Amberstar.Translator) (the command line tool).
