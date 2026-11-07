using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIReforge
{
    // ExtraSlots (shudnal) adds three HotkeyBar clones under Hud.m_rootObject:
    // ammo, food and quick slots. In the Yggdrasil style they are moved around the
    // HUD (ammo above the health bar, food right of the bars, quick slots under
    // the vanilla hotbar) and drawn like the Figma mockup: no dark tiles, a thin
    // white outline, white labels, flat glyphs for arrows and food.
    //
    // ExtraSlots is never referenced: bars are found by name, and its own config
    // is left alone. Our drawing sits in a "YggSkin" child of every element; the
    // vanilla graphics are only disabled (Graphic.enabled), so RestoreAll() brings
    // the original look back. Bar position and scale are restored the same way.
    internal static class ExtraSlotsSkin
    {
        internal const string AmmoBar = "ExtraSlotsAmmoHotBar";
        internal const string FoodBar = "ExtraSlotsFoodHotBar";
        internal const string QuickBar = "ExtraSlotsQuickSlotsHotBar";
        private const string VanillaBar = "HotKeyBar";
        private const string SkinName = "YggSkin";

        private enum Kind { Ammo, Food, Quick }

        private class BarState
        {
            public RectTransform Rect;
            public Vector3 Position;
            public Vector3 Scale;
        }

        private class ElementSkin
        {
            public GameObject Root;
            public RectTransform Skin;
            public Image Frame, Marks, Icon, Tip;
            public TextMeshProUGUI Binding, Amount;
            public Graphic Background;
            public readonly List<Graphic> Hidden = new List<Graphic>();
        }

        private static readonly Dictionary<RectTransform, BarState> Bars = new Dictionary<RectTransform, BarState>();
        private static readonly Dictionary<GameObject, ElementSkin> Skins = new Dictionary<GameObject, ElementSkin>();
        private static readonly Dictionary<Sprite, ItemDrop.ItemData> ItemsByIcon = new Dictionary<Sprite, ItemDrop.ItemData>();
        private static int itemsByIconSource = -1;

        private static FieldInfo elementsField;
        private static FieldInfo goField, iconField, amountField, equipedField, usedField;

        // Every style: quick slots under the vanilla hotbar, ammo and food rows
        // next to the HUD block (positions per style below).
        internal static bool Active =>
            HudState.Initialized && HudState.Panel != null &&
            HudUpdatePlugin.ExtraSlotsLayout != null && HudUpdatePlugin.ExtraSlotsLayout.Value == ExtraSlotsLayoutMode.Auto;

        private static bool Handles(Kind kind) => true;

        // First slot centre of the ammo and food rows, panel units of the active style.
        private static void RowStarts(out Vector2 ammo, out Vector2 food)
        {
            switch (HudState.ActiveStyle)
            {
                case HudStyle.Yggdrasil:
                    ammo = HudState.YggAmmoStart; food = HudState.YggFoodBarStart; return;
                case HudStyle.Chevron:
                    // Right of the chevron: above and below the 4th food diamond (C + (152, 0)).
                    ammo = new Vector2(300f, 185f); food = new Vector2(300f, 25f); return;
                default:
                    // Align the right edge of each three-slot row with the bars' value plate.
                    const float barsRight = 238f + 266f;
                    ammo = new Vector2(barsRight - HudState.YggSlotSize / 2f - 2f * HudState.YggAmmoPitch, 166f);
                    food = new Vector2(barsRight - HudState.YggSlotSize / 2f - 2f * HudState.YggFoodPitch, 44f);
                    return;
            }
        }

        private static bool TryKind(string barName, out Kind kind)
        {
            kind = Kind.Ammo;
            if (barName == AmmoBar) kind = Kind.Ammo;
            else if (barName == FoodBar) kind = Kind.Food;
            else if (barName == QuickBar) kind = Kind.Quick;
            else return false;
            return true;
        }

        // ---- element skin (after every HotkeyBar.UpdateIcons) ---------------
        internal static void AfterUpdateIcons(HotkeyBar bar)
        {
            if (bar == null || !TryKind(bar.name, out Kind kind)) return;
            if (!Active || !Handles(kind)) return;
            if (!BindFields()) return;
            var elements = elementsField.GetValue(bar) as IList;
            if (elements == null) return;

            var visible = new List<ElementSkin>();
            foreach (object element in elements)
            {
                var go = goField.GetValue(element) as GameObject;
                if (go == null) continue;
                var skin = GetSkin(go, kind);
                if (skin == null) continue;
                Dress(skin, element, kind);
                if (go.activeSelf) visible.Add(skin);
            }

            // One row, in the order ExtraSlots laid them out (left to right, top to bottom).
            visible.Sort((a, b) =>
            {
                Vector3 pa = a.Root.transform.localPosition, pb = b.Root.transform.localPosition;
                int row = -pa.y.CompareTo(pb.y);
                return Mathf.Abs(pa.y - pb.y) > 1f ? row : pa.x.CompareTo(pb.x);
            });
            float pitch = (kind == Kind.Food ? HudState.YggFoodPitch : HudState.YggAmmoPitch) / UnitsPerLocal(visible);
            for (int i = 0; i < visible.Count; i++)
                visible[i].Root.transform.localPosition = new Vector3(i * pitch, 0f, 0f);
        }

        // Element-local units per canvas unit: the vanilla slot tile is 64 wide and
        // is shown 45 wide, so local distances are scaled by 64/45.
        private static float UnitsPerLocal(List<ElementSkin> skins)
        {
            foreach (var s in skins)
            {
                float w = SlotWidth(s);
                if (w > 1f) return HudState.YggSlotSize / w;
            }
            return HudState.YggSlotSize / 64f;
        }

        private static float SlotWidth(ElementSkin skin)
        {
            var rt = skin.Background != null ? skin.Background.rectTransform : skin.Root.transform as RectTransform;
            return rt != null ? rt.rect.width : 64f;
        }

        private static ElementSkin GetSkin(GameObject go, Kind kind)
        {
            if (Skins.TryGetValue(go, out var skin) && skin.Skin != null) return skin;

            skin = new ElementSkin { Root = go };
            var bkg = go.transform.Find("bkg");
            skin.Background = bkg != null ? bkg.GetComponent<Graphic>() : null;
            var area = skin.Background != null ? skin.Background.rectTransform : go.transform as RectTransform;

            skin.Skin = new GameObject(SkinName, typeof(RectTransform)).GetComponent<RectTransform>();
            skin.Skin.SetParent(go.transform, false);
            if (area != null && area != go.transform)
            {
                skin.Skin.anchorMin = area.anchorMin;
                skin.Skin.anchorMax = area.anchorMax;
                skin.Skin.pivot = area.pivot;
                skin.Skin.anchoredPosition = area.anchoredPosition;
                skin.Skin.sizeDelta = area.sizeDelta;
            }
            else
            {
                skin.Skin.anchorMin = Vector2.zero;
                skin.Skin.anchorMax = Vector2.one;
                skin.Skin.offsetMin = skin.Skin.offsetMax = Vector2.zero;
            }

            float k = SlotWidthOf(skin.Skin) / HudState.YggSlotSize;   // local units per Figma px
            skin.Frame = NewImage("frame", skin.Skin, "ygg_slot", Vector2.zero, HudState.YggSlotSize * k * Vector2.one);
            skin.Marks = NewImage("marks", skin.Skin, "ygg_slot_marks", Vector2.zero, 61f * k * Vector2.one);
            skin.Icon = NewImage("icon", skin.Skin, null, Vector2.zero, 24f * k * Vector2.one);
            skin.Icon.preserveAspect = true;
            skin.Tip = NewImage("tip", skin.Icon.rectTransform, "ygg_ammo_tip", Vector2.zero, Vector2.zero);
            StretchFull(skin.Tip.rectTransform);

            var templateText = go.GetComponentInChildren<TextMeshProUGUI>(true);
            skin.Binding = NewText("binding", skin.Skin, templateText, 10f * k);
            float foodCountSize = HudState.ActiveStyle == HudStyle.Chevron ? 11f
                : HudState.ActiveStyle == HudStyle.Yggdrasil ? 10f : 14f;
            skin.Amount = NewText("amount", skin.Skin, templateText,
                (kind == Kind.Food ? foodCountSize : kind == Kind.Ammo ? 10f : 9f) * k);
            if (kind == Kind.Food && HudState.ActiveStyle == HudStyle.Chevron)
                skin.Amount.fontStyle = FontStyles.Normal;
            if (kind == Kind.Ammo)
            {
                PlaceText(skin.Binding, new Vector2(-1f, 9.5f) * k, new Vector2(44f, 14f) * k, TextAlignmentOptions.Center);
                PlaceText(skin.Amount, new Vector2(0f, -10.5f) * k, new Vector2(44f, 14f) * k, TextAlignmentOptions.Center);
                Place(skin.Icon.rectTransform, new Vector2(.5f, 0f) * k, new Vector2(32f, 5f) * k);
            }
            else
            {
                PlaceText(skin.Binding, new Vector2(.5f, 11.5f) * k, new Vector2(44f, 14f) * k, TextAlignmentOptions.Center);
                PlaceText(skin.Amount, new Vector2(kind == Kind.Food ? -3f : 1f, -11.5f) * k,
                    new Vector2(42f, kind == Kind.Food ? 18f : 12f) * k, TextAlignmentOptions.Right);
                Place(skin.Icon.rectTransform, new Vector2(.5f, -3.5f) * k, (kind == Kind.Food ? 21f : 24f) * k * Vector2.one);
            }
            skin.Skin.SetAsLastSibling();
            Skins[go] = skin;
            return skin;
        }

        private static float SlotWidthOf(RectTransform rt)
        {
            float w = rt.rect.width;
            return w > 1f ? w : 64f;
        }

        private static void Dress(ElementSkin skin, object element, Kind kind)
        {
            var icon = iconField.GetValue(element) as Image;
            var amount = amountField.GetValue(element) as TMP_Text;
            var equiped = equipedField.GetValue(element) as GameObject;
            bool used = usedField != null && (bool)usedField.GetValue(element);
            var binding = skin.Root.transform.Find("binding")?.GetComponent<TMP_Text>();

            // Read the vanilla equipped flag, but only switch its picture off: ExtraSlots
            // skips UpdateIcons when nothing changed and the flag must survive that.
            bool equipped = equiped != null && equiped.activeSelf;
            if (equiped != null)
                foreach (var g in equiped.GetComponentsInChildren<Graphic>(true)) Hide(skin, g);

            Hide(skin, skin.Background);
            Hide(skin, icon);
            Hide(skin, amount);
            Hide(skin, binding);

            skin.Skin.gameObject.SetActive(true);
            skin.Binding.text = binding != null ? binding.text : "";
            skin.Binding.enabled = binding != null && binding.gameObject.activeInHierarchy && !string.IsNullOrEmpty(binding.text);
            skin.Amount.text = amount != null ? CountOnly(amount.text) : "";
            skin.Amount.enabled = used && amount != null && amount.gameObject.activeSelf;

            Sprite sprite = used && icon != null && icon.gameObject.activeSelf ? icon.sprite : null;
            var item = sprite != null ? FindItem(sprite) : null;
            bool arrow = kind == Kind.Ammo && IsArrowLike(item, sprite);

            skin.Icon.enabled = sprite != null;
            skin.Tip.enabled = false;
            if (sprite != null)
            {
                if (arrow && HudState.FoodIconOverrides.TryGetValue("ygg_ammo", out var glyph))
                {
                    skin.Icon.sprite = glyph;
                    skin.Icon.preserveAspect = false;
                    skin.Icon.color = Color.white;
                    Color? tip = TipColor(item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : sprite.name);
                    skin.Tip.enabled = tip.HasValue;
                    if (tip.HasValue) skin.Tip.color = tip.Value;
                }
                else
                {
                    Sprite flat = kind == Kind.Food ? HudState.ResolveItemIcon(item, null) : null;
                    skin.Icon.sprite = flat ?? sprite;
                    skin.Icon.preserveAspect = true;
                    skin.Icon.color = icon != null ? icon.color : Color.white;
                }
            }

            // Food and quick slots always show their outline (like the Figma food row);
            // Empty ammo slots get a faint frame; equipped arrows retain bright marks.
            bool emptyAmmo = kind == Kind.Ammo && !used;
            bool framed = kind != Kind.Ammo || equipped || emptyAmmo;
            skin.Frame.enabled = framed && skin.Frame.sprite != null;
            skin.Marks.enabled = kind == Kind.Ammo && equipped && skin.Marks.sprite != null;
            if (kind != Kind.Ammo && equipped && skin.Frame.enabled) skin.Frame.color = new Color(.55f, .85f, 1f, 1f);
            else skin.Frame.color = emptyAmmo && !equipped ? new Color(.7f, .7f, .7f, .35f) : Color.white;
        }

        // "8 / 10" -> "8": the mockup shows only the count.
        private static string CountOnly(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int slash = text.IndexOf('/');
            return slash > 0 ? text.Substring(0, slash).Trim() : text;
        }

        private static void Hide(ElementSkin skin, Graphic graphic)
        {
            if (graphic == null || !graphic.enabled) return;
            graphic.enabled = false;
            if (!skin.Hidden.Contains(graphic)) skin.Hidden.Add(graphic);
        }

        // ---- bar placement (every frame, after ExtraSlots' Hud.Update) -------
        private static bool wasActive;

        internal static void AfterHudUpdate(Hud hud)
        {
            if (hud == null || hud.m_rootObject == null) return;
            if (!Active)
            {
                wasActive = false;
                if (Bars.Count > 0 || Skins.Count > 0) RestoreAll();
                return;
            }
            if (!wasActive)
            {
                // Dress the bars right away instead of waiting for an inventory change.
                wasActive = true;
                RefreshBars();
            }
            var parent = hud.m_rootObject.transform;
            var panel = HudState.Panel.transform as RectTransform;
            RowStarts(out Vector2 ammoStart, out Vector2 foodStart);
            Place(parent.Find(AmmoBar) as RectTransform, panel, ammoStart);
            Place(parent.Find(FoodBar) as RectTransform, panel, foodStart);
            PlaceQuick(parent.Find(QuickBar) as RectTransform, parent.Find(VanillaBar) as RectTransform, panel);
        }

        private static void Place(RectTransform bar, RectTransform panel, Vector2 firstSlot)
        {
            if (bar == null || !bar.gameObject.activeInHierarchy) return;
            var first = FirstSkin(bar);
            if (first == null) return;
            Remember(bar);
            float scale = panel.localScale.x;
            Vector3 target = panel.TransformPoint(new Vector3(firstSlot.x, firstSlot.y, 0f));
            Move(bar, first, target, scale);
        }

        private static void PlaceQuick(RectTransform bar, RectTransform hotbar, RectTransform panel)
        {
            if (bar == null || !bar.gameObject.activeInHierarchy) return;
            var first = FirstSkin(bar);
            if (first == null) return;
            Remember(bar);
            float scale = panel.localScale.x;
            Vector3 target;
            var hotbarSlot = hotbar != null ? FirstVanillaSlot(hotbar) : null;
            if (hotbarSlot != null)
            {
                // Under the first slot of the vanilla hotbar, left edges aligned.
                var corners = new Vector3[4];
                hotbarSlot.GetWorldCorners(corners);       // 0 = bottom-left
                float unit = bar.parent.lossyScale.y;
                target = corners[0] + new Vector3((HudState.YggSlotSize / 2f) * scale * unit,
                    -(10f + HudState.YggSlotSize / 2f) * scale * unit, 0f);
            }
            else
            {
                target = panel.TransformPoint(new Vector3(HudState.YggAmmoStart.x, HudState.YggAmmoStart.y + 60f, 0f));
            }
            Move(bar, first, target, scale);
        }

        private static RectTransform FirstVanillaSlot(RectTransform hotbar)
        {
            RectTransform best = null;
            float bestX = float.MaxValue;
            foreach (Transform child in hotbar)
            {
                if (!child.gameObject.activeSelf) continue;
                var bkg = child.Find("bkg") as RectTransform;
                if (bkg == null) continue;
                float x = bkg.position.x;
                if (x < bestX) { bestX = x; best = bkg; }
            }
            return best;
        }

        private static ElementSkin FirstSkin(RectTransform bar)
        {
            ElementSkin best = null;
            float bestX = float.MaxValue;
            foreach (Transform child in bar)
            {
                if (!child.gameObject.activeSelf) continue;
                if (!Skins.TryGetValue(child.gameObject, out var skin) || skin.Skin == null) continue;
                float x = child.localPosition.x;
                if (x < bestX) { bestX = x; best = skin; }
            }
            return best;
        }

        private static void Move(RectTransform bar, ElementSkin first, Vector3 target, float scale)
        {
            float slot = SlotWidthOf(first.Skin);
            float want = HudState.YggSlotSize / slot * scale;
            if (Mathf.Abs(bar.localScale.x - want) > .0001f) bar.localScale = new Vector3(want, want, 1f);
            Vector3 current = first.Skin.TransformPoint(first.Skin.rect.center);
            Vector3 delta = target - current;
            if (delta.sqrMagnitude > 1e-6f) bar.position += new Vector3(delta.x, delta.y, 0f);
        }

        private static void Remember(RectTransform bar)
        {
            if (Bars.ContainsKey(bar)) return;
            Bars[bar] = new BarState { Rect = bar, Position = bar.localPosition, Scale = bar.localScale };
        }

        // Back to ExtraSlots' own look and place (style changed or ExtraSlotsLayout = Off).
        internal static void RestoreAll()
        {
            foreach (var skin in Skins.Values)
            {
                foreach (var g in skin.Hidden)
                    if (g != null) g.enabled = true;
                skin.Hidden.Clear();
                if (skin.Skin != null) UnityEngine.Object.Destroy(skin.Skin.gameObject);
            }
            Skins.Clear();
            foreach (var state in Bars.Values)
            {
                if (state.Rect == null) continue;
                state.Rect.localPosition = state.Position;
                state.Rect.localScale = state.Scale;
            }
            Bars.Clear();
            // Element positions come back with the next refresh.
            RefreshBars();
        }

        private static readonly MethodInfo UpdateIconsMethod = AccessTools.Method(typeof(HotkeyBar), "UpdateIcons");

        private static void RefreshBars()
        {
            if (Hud.instance == null || Hud.instance.m_rootObject == null || Player.m_localPlayer == null) return;
            foreach (string name in new[] { AmmoBar, FoodBar, QuickBar })
            {
                var bar = Hud.instance.m_rootObject.transform.Find(name)?.GetComponent<HotkeyBar>();
                if (bar == null) continue;
                try { UpdateIconsMethod?.Invoke(bar, new object[] { Player.m_localPlayer }); }
                catch (Exception ex) { UnityEngine.Debug.LogWarning("[UIReforge] ExtraSlots refresh: " + ex.Message); }
            }
        }

        // ---- helpers ----------------------------------------------------------
        private static bool BindFields()
        {
            if (elementsField != null) return goField != null;
            elementsField = AccessTools.Field(typeof(HotkeyBar), "m_elements");
            var type = elementsField?.FieldType.IsGenericType == true ? elementsField.FieldType.GetGenericArguments()[0] : null;
            if (type == null) return false;
            goField = AccessTools.Field(type, "m_go");
            iconField = AccessTools.Field(type, "m_icon");
            amountField = AccessTools.Field(type, "m_amount");
            equipedField = AccessTools.Field(type, "m_equiped");
            usedField = AccessTools.Field(type, "m_used");
            bool ok = goField != null && iconField != null && amountField != null && equipedField != null;
            if (!ok)
            {
                UnityEngine.Debug.LogWarning("[UIReforge] HotkeyBar layout changed, ExtraSlots skin disabled");
                goField = null;
            }
            return ok;
        }

        private static ItemDrop.ItemData FindItem(Sprite icon)
        {
            var db = ObjectDB.instance;
            if (db == null || db.m_items == null) return null;
            if (itemsByIconSource != db.m_items.Count)
            {
                itemsByIconSource = db.m_items.Count;
                ItemsByIcon.Clear();
                foreach (var prefab in db.m_items)
                {
                    var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                    var shared = drop?.m_itemData?.m_shared;
                    if (shared?.m_icons == null) continue;
                    if (drop.m_itemData.m_dropPrefab == null) drop.m_itemData.m_dropPrefab = prefab;
                    foreach (var s in shared.m_icons)
                        if (s != null && !ItemsByIcon.ContainsKey(s)) ItemsByIcon[s] = drop.m_itemData;
                }
            }
            return ItemsByIcon.TryGetValue(icon, out var item) ? item : null;
        }

        private static bool IsArrowLike(ItemDrop.ItemData item, Sprite sprite)
        {
            string name = item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : sprite != null ? sprite.name : "";
            name = name.ToLowerInvariant();
            return name.Contains("arrow") || name.Contains("bolt");
        }

        // Coloured arrowhead for special ammo (Figma: fire arrow tip #ED6A31).
        private static Color? TipColor(string prefab)
        {
            string n = (prefab ?? "").ToLowerInvariant();
            if (n.Contains("fire")) return new Color(.93f, .42f, .19f);
            if (n.Contains("frost") || n.Contains("ice")) return new Color(.55f, .85f, 1f);
            if (n.Contains("poison")) return new Color(.48f, .85f, .29f);
            if (n.Contains("silver")) return new Color(.78f, .8f, .88f);
            if (n.Contains("carapace")) return new Color(.25f, .65f, .63f);
            if (n.Contains("charred")) return new Color(.55f, .25f, .85f);
            return null;
        }

        private static Image NewImage(string name, Transform parent, string spriteKey, Vector2 pos, Vector2 size)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            Place(image.rectTransform, pos, size);
            if (spriteKey != null && HudState.FoodIconOverrides.TryGetValue(spriteKey, out var sprite)) image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, TextMeshProUGUI template, float size)
        {
            TextMeshProUGUI text;
            if (template != null)
            {
                text = UnityEngine.Object.Instantiate(template, parent, false);
                foreach (Transform child in text.transform) UnityEngine.Object.Destroy(child.gameObject);
            }
            else
            {
                text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                text.transform.SetParent(parent, false);
            }
            text.name = name;
            text.enabled = true;
            text.gameObject.SetActive(true);
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.outlineColor = new Color32(12, 10, 8, 255);
            text.outlineWidth = .2f;
            return text;
        }

        private static void PlaceText(TextMeshProUGUI text, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            Place(text.rectTransform, pos, size);
            text.alignment = align;
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
    internal static class ExtraSlotsUpdateIconsPatch
    {
        [HarmonyAfter("shudnal.ExtraSlots")]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(HotkeyBar __instance)
        {
            try { ExtraSlotsSkin.AfterUpdateIcons(__instance); }
            catch (Exception ex) { UnityEngine.Debug.LogError("[UIReforge] ExtraSlots skin: " + ex); }
        }
    }

    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class ExtraSlotsHudUpdatePatch
    {
        [HarmonyAfter("shudnal.ExtraSlots")]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Hud __instance)
        {
            try { ExtraSlotsSkin.AfterHudUpdate(__instance); }
            catch (Exception ex) { UnityEngine.Debug.LogError("[UIReforge] ExtraSlots layout: " + ex); }
        }
    }
}
