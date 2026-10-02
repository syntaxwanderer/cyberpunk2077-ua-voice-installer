# Cyberpunk 2077 — Ukrainian Voice-Over (AI): installer source

Source code of `UA_Voice_Setup.exe`, the one-click installer shipped inside the Nexus Mods package
**Ukrainian Voice-Over (AI) - Ukrainska Ozvuchka** (Cyberpunk 2077, mod 34398):
https://www.nexusmods.com/cyberpunk2077/mods/34398

Українська: це відкритий код інсталятора фанатської української ШІ-озвучки Cyberpunk 2077.
Інсталятор нічого не завантажує з інтернету — лише копіює файл озвучки в теку гри,
перевіряє його контрольну суму та вмикає мову озвучення «Українська (ШІ)».

## What is in the mod package

| File in the zip | What it is |
|---|---|
| `archive\pc\mod\UA_Voice.archive` | Game data only: Wwise audio (`.wem`) of the voiced lines plus the voice-language map / lipsync resources, packed with [WolvenKit](https://github.com/WolvenKit/WolvenKit). Contains no executable code; it can be opened and inspected with WolvenKit. |
| `UA_Voice_Setup.exe` | The installer built from this repository (C#, .NET Framework 4.x, WinForms). |
| `installer_source\` | The same `Setup.cs`, `Program.cs`, `build.cmd` as here. |
| `README_UA_Voice.txt` | Installation instructions. |

## What the installer does

Everything is in `Setup.cs` (logic) and `Program.cs` (window + command line):

1. **Finds the game** (`Job.FindGame`): reads the Steam path from `HKCU\Software\Valve\Steam` and `libraryfolders.vdf`,
   GOG entries under `HKLM\SOFTWARE\(WOW6432Node\)GOG.com\Games`, Epic manifests in
   `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests`, and a few default folders.
   A folder is accepted only if it contains `bin\x64\Cyberpunk2077.exe` and `archive\pc\content`.
   The user can also pick the folder manually.
2. **Installs** (`Job.Install`): copies `UA_Voice.archive` from the unpacked package into
   `<game>\archive\pc\mod\` (via a temporary file), verifies size + CRC32 of the copy,
   removes an old test file `UA_Voice_pilot.archive` if present, and warns about other voice/language mods.
3. **Switches the voice-over language** (`Job.SetVoice`): in
   `%LocalAppData%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` it changes only the `value` of the
   `VoiceOver` option to `es-mx` (the free slot used for "Українська (ШІ)"), after making a backup
   `UserSettings.json.bak_ua`. The rest of the file is left byte-for-byte unchanged.
4. **Uninstalls** (`Job.Uninstall`): deletes the mod archive(s) and restores the previous voice-over language from the backup.

It does **not**: access the network, start or inject into the game, write to the registry, install services,
create scheduled tasks, request admin rights, or touch any file outside the game's `archive\pc\mod` folder and `UserSettings.json`
(+ its `.bak_ua` backup).

## How to build

Requirements: Windows 10/11 (the .NET Framework 4.x C# compiler `csc.exe` ships with Windows). No Visual Studio, NuGet or internet needed.

```
git clone https://github.com/syntaxwanderer/cyberpunk2077-ua-voice-installer.git
cd cyberpunk2077-ua-voice-installer
build.cmd
```

`build.cmd` runs:

```
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /optimize+ /platform:anycpu ^
  /target:winexe /out:UA_Voice_Setup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll Setup.cs Program.cs
```

and the same with `/target:exe /out:UA_Voice_Setup_cli.exe` (console build used for automated tests).
Output: `UA_Voice_Setup.exe` (~20 KB). The released 0.5 binary was built this way with csc 4.8.9221.0
(SHA-256 of the exe in the 0.5 package: `5b0bc4cdd5bfc81005557d1a9fe1c44d20258f5533bf1a31369a03ce2a0b2f36`).
The C# compiler embeds a build timestamp/MVID, so a rebuild will not be byte-identical but is functionally the same code.

## Test without a window

```
UA_Voice_Setup_cli.exe --cli [--game "<game folder>"] [--uninstall] [--test]
```

Put the exe next to the unpacked package (`archive\pc\mod\UA_Voice.archive`). `--test` skips the "game is running" check.
Exit code 0 = OK.

## About the mod

Fan-made, free and non-commercial. Voices are AI-generated (FireRedTTS, Higgs Audio, Seed-VC); acting is transferred
per line from the game's Polish dub. Not approved or endorsed by CD PROJEKT RED.
Cyberpunk 2077 © CD PROJEKT S.A.
