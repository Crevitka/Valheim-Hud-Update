HUD Update - icons
==================

Every icon the HUD draws is a PNG in this folder. Replace a file with your own
picture under the same name and the HUD picks it up within a second, even while
the game is running. Delete a file to get the built-in version back.

Keep your own icons safe from updates
-------------------------------------
A mod update (or a reinstall from a mod manager) restores this folder. Put your
replacements into

    BepInEx/config/HUD-Update/Icons

instead. That folder is created on the first launch and always wins over this one.

Names
-----
food_*.png          food glyphs shared by many dishes (food_steak, food_stew, food_pie...)
<FoodPrefab>.png    an icon for one exact food, by prefab name: CookedMeat.png, OnionSoup.png
Eikthyr.png, TheElder.png, Bonemass.png
                    guardian emblems
GP_<Power>.png      an emblem for any other guardian power: GP_Moder.png, GP_Yagluth.png,
                    GP_Queen.png, GP_Fader.png
HealthIcon.png, StaminaIcon.png
                    the + and lightning signs next to the bars (Bars style)
chevron_shape.png, chevron_outline.png
                    the red health chevron (Chevron style)

Tips
----
- Square PNGs with a transparent background work best, 128x128 or larger.
- Food icons are drawn white in the base look. FoodIconColor = Tint multiplies the
  icon by red / yellow / blue, so it looks right only on white or light icons.
- Errors (for example a broken PNG) are written to BepInEx/LogOutput.log with the
  [UIReforge] prefix; the built-in icon is used instead.

Guardian emblems (Eikthyr, The Elder, Bonemass): 𝗛คηɗץ.
