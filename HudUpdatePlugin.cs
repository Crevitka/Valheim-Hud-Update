using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIReforge
{
    [BepInPlugin("crevitka.hudupdate", "Hud Update", "1.0.0")]
    public class HudUpdatePlugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            try
            {
                _harmony = new Harmony("crevitka.hudupdate");
                _harmony.PatchAll();

                Logger.LogInfo("UIReforge loaded");
            }
            catch (Exception ex)
            {
                Logger.LogError($"UIReforge init failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Logger.LogError($"UIReforge unpatch failed: {ex}");
            }
        }
    }

    internal static class TmpFontFix
    {
        public static void ApplyFontsToPrefab(GameObject prefab)
        {
            if (prefab == null) return;

            try
            {
                var texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    if (text == null) continue;

                    if (text.font == null)
                    {
                        var defaultFont = TMP_Settings.defaultFontAsset;
                        if (defaultFont != null)
                            text.font = defaultFont;
                    }

                    text.ForceMeshUpdate();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] ApplyFontsToPrefab error: " + ex);
            }
        }

        public static void FixFontsOnInstance(GameObject instance)
        {
            if (instance == null) return;

            try
            {
                var texts = instance.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    if (text == null) continue;

                    if (text.font == null)
                    {
                        var defaultFont = TMP_Settings.defaultFontAsset;
                        if (defaultFont != null)
                            text.font = defaultFont;
                    }

                    text.ForceMeshUpdate();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] FixFontsOnInstance error: " + ex);
            }
        }
    }

    internal static class HudState
    {
        internal class FoodSlotUI
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Timer;
            public Image VanillaIcon;
        }

        internal static bool Initialized;
        internal static GameObject Panel;

        internal static Image HpFastImage;
        internal static Image HpSlowImage;
        internal static TextMeshProUGUI HpText;
        internal static float HpFastValue = -1f;
        internal static float HpSlowValue = -1f;

        internal static Image StaminaFastImage;
        internal static Image StaminaSlowImage;
        internal static TextMeshProUGUI StaminaText;
        internal static float StaminaFastValue = -1f;
        internal static float StaminaSlowValue = -1f;

        internal static readonly FoodSlotUI[] Slots = new FoodSlotUI[3];
        internal static Sprite DefaultFoodIcon;

        internal static AssetBundle HudBundle;
        internal static AssetBundle FoodIconsBundle;

        internal static readonly Dictionary<string, Sprite> FoodIconOverrides =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        internal const float TimerYOffset = -28f;

        internal static void EnsureInit(Hud hud)
        {
            if (Initialized) return;
            Initialized = true;

            try
            {
                string hudBundlePath = Path.Combine(Paths.PluginPath, "prefabs", "hudPrefab");
                HudBundle = AssetBundle.LoadFromFile(hudBundlePath);

                if (HudBundle == null)
                {
                    UnityEngine.Debug.LogError("[UIReforge] Failed to load HUD bundle: " + hudBundlePath);
                    return;
                }

                GameObject prefab = HudBundle.LoadAsset<GameObject>("healthpanel");
                if (prefab == null)
                {
                    UnityEngine.Debug.LogError("[UIReforge] Failed to load prefab 'healthpanel' from bundle");
                    return;
                }

                DefaultFoodIcon = HudBundle.LoadAsset<Sprite>("default-food-icon");

                LoadFoodIconsBundle();
                TmpFontFix.ApplyFontsToPrefab(prefab);

                Panel = UnityEngine.Object.Instantiate(prefab, hud.m_rootObject.transform);
                Panel.name = "CustomHealthPanel";

                TmpFontFix.FixFontsOnInstance(Panel);
                FindReferences(Panel);

                if (hud.m_healthPanel != null)
                    hud.m_healthPanel.gameObject.SetActive(false);

                HideVanillaStamina(hud);

                UnityEngine.Debug.Log("[UIReforge] Custom HUD initialized");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] Init error: " + ex);
            }
        }

        internal static void LoadFoodIconsBundle()
        {
            try
            {
                FoodIconOverrides.Clear();

                string foodBundlePath = Path.Combine(Paths.PluginPath, "prefabs", "food_icons");
                if (!File.Exists(foodBundlePath))
                    return;

                FoodIconsBundle = AssetBundle.LoadFromFile(foodBundlePath);
                if (FoodIconsBundle == null)
                {
                    UnityEngine.Debug.LogError("[UIReforge] Failed to load food_icons bundle");
                    return;
                }

                var sprites = FoodIconsBundle.LoadAllAssets<Sprite>();
                foreach (var sp in sprites)
                {
                    if (sp != null)
                        FoodIconOverrides[sp.name] = sp;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] LoadFoodIconsBundle error: " + ex);
            }
        }

        internal static void FindReferences(GameObject root)
        {
            FindBarGroup(root.transform.Find("Health"), out HpFastImage, out HpSlowImage, out HpText);
            FindBarGroup(root.transform.Find("Stamina"), out StaminaFastImage, out StaminaSlowImage, out StaminaText);

            var foodChildren = new List<Transform>();
            foreach (Transform child in root.transform)
            {
                if (child.name.IndexOf("food", StringComparison.OrdinalIgnoreCase) >= 0)
                    foodChildren.Add(child);
            }

            foodChildren.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            for (int i = 0; i < Slots.Length; i++)
            {
                if (i >= foodChildren.Count)
                {
                    Slots[i] = null;
                    continue;
                }

                Transform slotTr = foodChildren[i];
                var slot = new FoodSlotUI
                {
                    Root = slotTr.gameObject,
                    Timer = slotTr.GetComponentInChildren<TextMeshProUGUI>(true)
                };

                slot.VanillaIcon = FindVanillaFoodIconImage(slotTr);
                if (slot.VanillaIcon != null)
                    slot.VanillaIcon.enabled = false;

                GameObject iconGO = new GameObject("UIReforgeIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(slotTr, false);

                var rt = iconGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                slot.Icon = iconGO.GetComponent<Image>();
                slot.Icon.raycastTarget = false;
                slot.Icon.preserveAspect = true;
                slot.Icon.transform.SetAsLastSibling();

                if (slot.Timer != null)
                {
                    var trt = slot.Timer.GetComponent<RectTransform>();
                    trt.anchoredPosition = new Vector2(0f, TimerYOffset);
                    slot.Timer.transform.SetAsLastSibling();
                }

                Slots[i] = slot;
            }
        }

        internal static void FindBarGroup(
            Transform groupRoot,
            out Image fastImage,
            out Image slowImage,
            out TextMeshProUGUI text)
        {
            fastImage = null;
            slowImage = null;
            text = null;

            if (groupRoot == null) return;

            Transform fastTr = groupRoot.Find("Mask/fast/bar");
            Transform slowTr = groupRoot.Find("Mask/slow/bar");

            if (fastTr != null)
            {
                fastImage = fastTr.GetComponent<Image>();
                SetupFilledBar(fastImage);
            }

            if (slowTr != null)
            {
                slowImage = slowTr.GetComponent<Image>();
                SetupFilledBar(slowImage);
            }

            text = groupRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        internal static void SetupFilledBar(Image img)
        {
            if (img == null) return;

            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;

            if (img.sprite == null && img.mainTexture is Texture2D tex)
            {
                img.sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
            }
        }

        internal static void HideVanillaStamina(Hud hud)
        {
            string[] names = { "Stamina", "stamina", "StaminaBar", "StaminaPanel" };

            foreach (var tr in hud.m_rootObject.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in names)
                {
                    if (string.Equals(name, tr.name, StringComparison.OrdinalIgnoreCase) &&
                        (Panel == null || !tr.IsChildOf(Panel.transform)))
                    {
                        tr.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        internal static Image FindVanillaFoodIconImage(Transform slotTr)
        {
            foreach (var img in slotTr.GetComponentsInChildren<Image>(true))
            {
                if (img.name == "UIReforgeIcon")
                    continue;

                var rt = img.rectTransform;

                if (rt.anchorMin == Vector2.zero &&
                    rt.anchorMax == Vector2.one &&
                    rt.offsetMin == Vector2.zero &&
                    rt.offsetMax == Vector2.zero)
                    continue;

                if (img.preserveAspect || img.name.ToLower().Contains("icon"))
                    return img;
            }

            return null;
        }

        internal static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return total >= 60 ? $"{Mathf.CeilToInt(total / 60f)}m" : $"{total}s";
        }

        internal static Sprite ResolveFoodIcon(Player.Food food)
        {
            var item = food?.m_item;
            if (item == null)
                return DefaultFoodIcon;

            if (item.m_shared != null &&
                FoodIconOverrides.TryGetValue(item.m_shared.m_name, out var bySharedName))
                return bySharedName;

            if (item.m_dropPrefab != null &&
                FoodIconOverrides.TryGetValue(item.m_dropPrefab.name, out var byPrefabName))
                return byPrefabName;

            if (item.m_shared != null &&
                item.m_shared.m_icons != null &&
                item.m_shared.m_icons.Length > 0)
                return item.m_shared.m_icons[0];

            return DefaultFoodIcon;
        }
    }

    [HarmonyPatch]
    internal static class HudHealthPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Hud), "UpdateHealth");
        }

        static bool Prefix(Hud __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return true;

            HudState.EnsureInit(__instance);
            if (HudState.Panel == null) return true;

            float hp = player.GetHealth();
            float maxHp = Mathf.Max(1f, player.GetMaxHealth());
            float target = Mathf.Clamp01(hp / maxHp);

            if (HudState.HpFastValue < 0f)
            {
                HudState.HpFastValue = target;
                HudState.HpSlowValue = target;
            }

            float dt = Time.deltaTime;

            if (target < HudState.HpFastValue)
                HudState.HpFastValue = target;

            if (target > HudState.HpSlowValue)
                HudState.HpSlowValue = target;

            if (HudState.HpSlowValue > HudState.HpFastValue)
                HudState.HpSlowValue = Mathf.MoveTowards(HudState.HpSlowValue, HudState.HpFastValue, 0.3f * dt);

            if (HudState.HpFastValue < HudState.HpSlowValue)
                HudState.HpFastValue = Mathf.MoveTowards(HudState.HpFastValue, HudState.HpSlowValue, 0.15f * dt);

            if (HudState.HpFastImage != null)
                HudState.HpFastImage.fillAmount = HudState.HpFastValue;

            if (HudState.HpSlowImage != null)
                HudState.HpSlowImage.fillAmount = HudState.HpSlowValue;

            if (HudState.HpText != null)
                HudState.HpText.text = Mathf.RoundToInt(hp).ToString();

            return false;
        }
    }

    [HarmonyPatch]
    internal static class HudStaminaPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Hud), "UpdateStamina");
        }

        static bool Prefix(Hud __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return true;

            HudState.EnsureInit(__instance);
            if (HudState.Panel == null) return true;

            float stamina = player.GetStamina();
            float maxStamina = Mathf.Max(1f, player.GetMaxStamina());
            float target = Mathf.Clamp01(stamina / maxStamina);

            if (HudState.StaminaFastValue < 0f)
            {
                HudState.StaminaFastValue = target;
                HudState.StaminaSlowValue = target;
            }

            float dt = Time.deltaTime;

            if (target < HudState.StaminaFastValue)
                HudState.StaminaFastValue = target;

            if (target > HudState.StaminaSlowValue)
                HudState.StaminaSlowValue = target;

            if (HudState.StaminaSlowValue > HudState.StaminaFastValue)
                HudState.StaminaSlowValue = Mathf.MoveTowards(HudState.StaminaSlowValue, HudState.StaminaFastValue, 1.2f * dt);

            if (HudState.StaminaFastValue < HudState.StaminaSlowValue)
                HudState.StaminaFastValue = Mathf.MoveTowards(HudState.StaminaFastValue, HudState.StaminaSlowValue, 0.6f * dt);

            if (HudState.StaminaFastImage != null)
                HudState.StaminaFastImage.fillAmount = HudState.StaminaFastValue;

            if (HudState.StaminaSlowImage != null)
                HudState.StaminaSlowImage.fillAmount = HudState.StaminaSlowValue;

            if (HudState.StaminaText != null)
                HudState.StaminaText.text = Mathf.RoundToInt(stamina).ToString();

            return false;
        }
    }

    [HarmonyPatch]
    internal static class HudFoodPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Hud), "UpdateFood");
        }

        static bool Prefix(Hud __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return true;

            HudState.EnsureInit(__instance);
            if (HudState.Panel == null) return true;

            List<Player.Food> foods = player.GetFoods();

            for (int i = 0; i < HudState.Slots.Length; i++)
            {
                var slot = HudState.Slots[i];
                if (slot == null || slot.Root == null)
                    continue;

                if (i < foods.Count)
                {
                    Player.Food food = foods[i];
                    Sprite icon = HudState.ResolveFoodIcon(food);

                    if (slot.Icon != null)
                    {
                        slot.Icon.sprite = icon;
                        slot.Icon.enabled = true;
                        slot.Icon.color = Color.white;
                    }

                    if (slot.Timer != null)
                    {
                        slot.Timer.text = HudState.FormatTime(food.m_time);
                        slot.Timer.enabled = true;
                    }
                }
                else
                {
                    if (slot.Icon != null)
                    {
                        slot.Icon.sprite = HudState.DefaultFoodIcon;
                        slot.Icon.enabled = HudState.DefaultFoodIcon != null;
                    }

                    if (slot.Timer != null)
                    {
                        slot.Timer.text = string.Empty;
                        slot.Timer.enabled = false;
                    }
                }
            }

            return false;
        }
    }
}