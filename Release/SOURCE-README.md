# HUD Update — source

Author: Crevitka. Guardian emblems (Eikthyr, The Elder, Bonemass): 𝗛คηɗץ. MIT License. Version 0.1.2.

## Build

On Windows, install a .NET SDK, Valheim and BepInExPack Valheim. Open PowerShell in this folder:

```powershell
./build-plugin.ps1 -ValheimDir 'D:\Games\Valheim'
```

The script references `valheim_Data/Managed` and `BepInEx/core` from the selected game folder (`-BepInExDir` selects another BepInEx). Without parameters it uses the standard Steam path under Program Files (x86). Output: `bin/Release/HUD-Update.dll`. Game, Unity and BepInEx assemblies are reference-only and must come from your own installation.

## Layout

- `HudUpdatePlugin.cs` — BepInEx entry point, config, Harmony prefixes on `Hud.UpdateHealth`, `Hud.UpdateStamina` and `Hud.UpdateFood`, and both HUD layouts.
- `TmpFontFixPatch.cs` — assigns the game's TextMeshPro font to the prefab texts.
- `Icons/*.png` — shipped next to the DLL in `Icons/` and also embedded as `UIReforge.Icons.<name>` (fallback; files in `BepInEx/config/HUD-Update/Icons` and the plugin `Icons/` folder win): food glyphs (`food_*`), guardian emblems (Eikthyr, TheElder, Bonemass), ✚ / ⚡, chevron frame.
- `Bundles/hudPrefab` — Unity asset bundle with the panel prefab, embedded as `UIReforge.Bundles.hudPrefab` (a loose `prefabs/hudPrefab` next to the DLL is still accepted as a fallback).
- `IconSource/` — Python generators for the food icons and the chevron (`generate_food_icons.py`, `chevron.py`); the chevron outline comes from the Figma layer in `figma_subtract_path.txt`.

The plugin GUID is `crevitka.hudupdate`. In-game verification is required after changing UI or hooks. Developed with AI assistance.
