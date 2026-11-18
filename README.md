# Amberstar.net

Remake of the Amiga classic Amberstar.

**Note: This is still in early development. Many things will change and be added.**

Ideas and feature suggestions are welcome in the issue tracker.

See the [changelog](CHANGELOG.md) for the changes of each version.

## Game versions

The releases for Windows (x64 and x86), Linux (x64) and macOS (Apple Silicon and Intel) contain the English and German game data
of the Amiga, Atari ST and DOS versions. They are packed into an archive which is appended to the executable, so the game runs
without extra files. At the start a version can be chosen (each version has its own savegames).

On macOS the archive is the file `versions.dat` next to the executable (appended data would break the signature). The executable
isn't notarized, so macOS blocks it after the download. Allow it in the system settings (Privacy & Security) or remove the
quarantine attribute with `xattr -dr com.apple.quarantine <folder>`.

Savegames and settings are stored in the folder `Saves` next to the executable. Without write permissions there they are
stored in `Amberstar.net/Saves` of the application data of the user (e.g. `%APPDATA%` on Windows, `~/.config` on Linux).
If there are no savegames or settings next to the executable but in the application data (e.g. from older versions), they are
copied next to the executable.

The archive is created and appended with [Amberstar.VersionPacker](#amberstarversionpacker). Instead of appending it, it can also
be put next to the executable as `versions.dat`. Without packed versions the game uses the data folders of the repository
(or the folder given by `--data`).

## Engine

The game is based on the Amber engine (https://github.com/Pyrdacor/Amber). Its projects are referenced as NuGet packages
(`Amber.Common`, `Amber.IO.FileSystem`, `Amber.Renderer.OpenGL`, ...). Their version is set by `AmberVersion` in
[Directory.Build.props](Directory.Build.props).

## Projects

### Amberstar.GameData

Definitions for Amberstar game data.

### Amberstar.GameData.Legacy

Concrete loaders and implementations for Amberstar game data which are related to the original legacy games for Atari ST, Amiga or DOS.

### Amberstar.Game

The game logic, screens and UI.

### Amberstar.Frontend

Creates the game for a renderer and game data. Also contains the cheats, the headless runner and the scenario scripts which are shared by the window frontend and the tests.

### Amberstar.net

The game with a window, OpenGL rendering and OpenAL audio. Use `--help` for the command line options (e.g. `--script` and `--headless`).

### Amberstar.VersionPacker

Packs game data folders into the game version archive and appends it to the executable:

```
dotnet run --project Amberstar.VersionPacker -- pack versions.dat
dotnet run --project Amberstar.VersionPacker -- embed path/to/Amberstar.net.exe versions.dat
```

Without more arguments `pack` uses the English and German Amiga, Atari ST and DOS data of the repository. The release workflow
([.github/workflows/release.yml](.github/workflows/release.yml)) does this for a tag like `v0.1.0-alpha` (native AOT builds for Windows and Linux).

### Amberstar.Translator

Extracts the texts of the game data for translators (gettext PO file) and creates the game data of a new language
from a translation:

```
dotnet run --project Amberstar.Translator -- extract --output amberstar.pot
dotnet run --project Amberstar.Translator -- check English/AmberfilesAmiga fr.po
dotnet run --project Amberstar.Translator -- create English/AmberfilesAmiga fr.po French/AmberfilesAmiga
```

See [Docs/Translation.md](Docs/Translation.md).

### Amberstar.Tests

Automated tests: loading the game data of all versions in the repository, game logic in a headless game and all scenario scripts in [Tests/Scenarios](Tests/Scenarios/README.md). Run them with `dotnet test Amberstar.Tests`.

### Amberstar

A small tool which exports the texts of the game data.

## Documentation

Notes about the parts of the original which are not in the released source code are in [Docs](Docs).
