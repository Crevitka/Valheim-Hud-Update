# Changelog

## 0.1.2

- Replaceable icons: every icon ships as a PNG in `BepInEx/plugins/HUD-Update/Icons`; edit or replace a file and the HUD reloads it in game. Personal replacements go to `BepInEx/config/HUD-Update/Icons` and survive updates. The DLL keeps built-in copies as a fallback.
- `GP_<Power>.png` adds an emblem for any guardian power (Moder, Yagluth, the Queen, Fader).
- Guardian emblem credit: 𝗛คηɗץ.
- README: one comparison picture for both styles and food colouring, plus every food icon next to the vanilla ones it replaces.

## 0.1.1

- Guardian emblems for The Elder and Bonemass (matched by guardian power, works in every game language).
- Before/after screenshots in the README.

## 0.1.0 — first GitHub release

- Nordic HUD block: guardian power diamond with Eikthyr emblem, name and cooldown; three food diamonds with timers.
- 23 flat white food icons with name-based matching for vanilla and modded foods.
- Vanilla-style pulse when a food can be eaten again.
- `HudStyle`: `Bars` (slanted health/stamina bars) or `Chevron` (2×2 food grid in a red health chevron, vanilla stamina).
- `FoodIconColor` / `FoodGlowStrength`: optional red/yellow/blue/white tint or glow by the food's dominant stat (off by default).
- Crisp dark rim on food timers.
- The HUD panel and all icons are embedded in the DLL; config changes apply live.
