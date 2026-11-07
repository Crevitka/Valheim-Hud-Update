# Changelog

## 0.2.0

- Added UseVanillaFoodIcons to use original food icons in HUD diamonds and ExtraSlots food slots, with live switching (default false).

- Added BarBackgroundOpacity for health/stamina tracks and value plates in Bars and Yggdrasil (default 0.75), applied live.

- Added the Yggdrasil HUD style and support for a fourth food slot.
- Added outlined guardian emblems for Moder, Yagluth, the Queen and Fader.
- Added optional ExtraSlots styling and layout integration; ExtraSlots is not required.
- Enlarged food quick-slot counts to match food timers, with more space from the right edge.
- Added faint outlines to empty ammo slots and aligned ammo/food rows with the right edge of the bars in Bars mode.
- Added HUD scale, diamond opacity and percentage/absolute bar scaling settings.
- Added an optional stamina strip for Chevron (off by default); its visual redesign is still pending.
- Fixed the source archive to include ExtraSlotsSkin.cs.

## 0.1.3

- Seven new food icons: feast, potato, pancake, cupcake, kale, oat and glow worm. Every vanilla food now gets a HUD Update icon, the Deep North included.
- Fixed: the Meadows feast showed the drink icon (its name contains "mead"). All nine feasts use the new feast icon.
- Deep North dishes mapped to fitting icons: oat milk and lingondricka (drink), moose kebab (skewer), pulled bear, seal blubber, moose and bjorn meat (steak), fish and seal soup, oatmeal with lingonberry jam (stew).

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
