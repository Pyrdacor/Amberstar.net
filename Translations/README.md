# Translations

| File | Content |
| --- | --- |
| [amberstar.pot](amberstar.pot) | The template with all English texts (created with `Amberstar.Translator extract`) |
| [cs.po](cs.po), [cs.glyphs.txt](cs.glyphs.txt) | Czech: the map, item and puzzle texts, the names and the UI texts are translated |
| [fr.po](fr.po), [fr.glyphs.txt](fr.glyphs.txt) | French: suggestions of the Ambermoon translation (fuzzy) |
| [pl.po](pl.po), [pl.glyphs.txt](pl.glyphs.txt) | Polish: suggestions of the Ambermoon translation (fuzzy) |

A translation is a `.po` file (one per language), its glyph file has the glyphs of the characters which the fonts
of the game don't have. See [Docs/Translation.md](../Docs/Translation.md) for the workflow and the notes for the
translators.

```
# check a translation (the glyph file next to it is used)
dotnet run --project Amberstar.Translator -- check English/AmberfilesAmiga Translations/cs.po

# create the game data of the language
dotnet run --project Amberstar.Translator -- create English/AmberfilesAmiga Translations/cs.po Czech/AmberfilesAmiga

# after the texts of the game data or the tool changed
dotnet run --project Amberstar.Translator -- extract --output Translations/amberstar.pot
dotnet run --project Amberstar.Translator -- update Translations/cs.po Translations/amberstar.pot
```
