using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[assembly: AssemblyTitle("HUD Update")]
[assembly: AssemblyDescription("Nordic HUD for Valheim: guardian power, food diamonds, health and stamina.")]
[assembly: AssemblyCompany("Crevitka")]
[assembly: AssemblyProduct("HUD Update")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Crevitka, MIT License")]
[assembly: AssemblyVersion("0.1.2.0")]
[assembly: AssemblyFileVersion("0.1.2.0")]

namespace UIReforge
{
    public enum HudStyle
    {
        Bars,
        Chevron
    }

    public enum FoodIconColorMode
    {
        Off,
        Tint,
        Glow,
        TintAndGlow
    }

    [BepInPlugin("crevitka.hudupdate", "HUD Update", "0.1.2")]
    public class HudUpdatePlugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        internal static ConfigEntry<HudStyle> Style;
        internal static ConfigEntry<FoodIconColorMode> FoodColor;
        internal static ConfigEntry<float> FoodGlowStrength;
        private DateTime _configStamp;
        private float _nextConfigCheck;
        private string _iconStamp;

        private void Awake()
        {
            try
            {
                Style = Config.Bind("General", "HudStyle", HudStyle.Bars,
                    "Bars - guardian, food diamonds and straight health/stamina bars.\n" +
                    "Chevron - guardian, 2x2 food grid and a red health chevron; stamina stays vanilla.\n" +
                    "Can be changed while the game runs: save this file and the HUD rebuilds.");
                Style.SettingChanged += (sender, args) => HudState.RequestRebuild();
                FoodColor = Config.Bind("Food", "FoodIconColor", FoodIconColorMode.Off,
                    "Colour food icons by the stat they raise most: health = red, stamina = yellow, eitr = blue, balanced = white.\n" +
                    "Off - plain icons. Tint - the icon itself is coloured (only the mod's white icons, vanilla icons stay as is).\n" +
                    "Glow - a soft coloured glow around the icon. TintAndGlow - both.");
                FoodGlowStrength = Config.Bind("Food", "FoodGlowStrength", .7f,
                    new ConfigDescription("Glow opacity, 0 - invisible, 1 - strongest.", new AcceptableValueRange<float>(0f, 1f)));
                _configStamp = SafeStamp();
                try { Directory.CreateDirectory(HudState.IconFolders[0]); } catch { }

                _harmony = new Harmony("crevitka.hudupdate");
                _harmony.PatchAll();

                Logger.LogInfo("UIReforge loaded");
            }
            catch (Exception ex)
            {
                Logger.LogError($"UIReforge init failed: {ex}");
            }
        }

        private DateTime SafeStamp()
        {
            try { return File.GetLastWriteTimeUtc(Config.ConfigFilePath); }
            catch { return DateTime.MinValue; }
        }

        // BepInEx does not watch the .cfg on its own; reload it when it is saved.
        private void Update()
        {
            if (Time.unscaledTime < _nextConfigCheck) return;
            _nextConfigCheck = Time.unscaledTime + 1f;
            string icons = HudState.IconFolderStamp();
            if (_iconStamp == null) _iconStamp = icons;
            else if (icons != _iconStamp)
            {
                _iconStamp = icons;
                HudState.ReloadIcons();
            }
            var stamp = SafeStamp();
            if (stamp == _configStamp) return;
            _configStamp = stamp;
            try { Config.Reload(); }
            catch (Exception ex) { Logger.LogWarning("Config reload failed: " + ex.Message); }
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

    internal static class HudState
    {
        internal class FoodSlotUI
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Timer;
            public TextMeshProUGUI[] TimerShadow;
            public Image VanillaIcon;
            public Image Glow;
        }

        internal class BuffUI
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Timer;
        }

        internal static TextMeshProUGUI GuardianName;
        private static float nextInitAttempt;
        internal static HudStyle ActiveStyle = HudStyle.Bars;
        private static readonly List<GameObject> HiddenVanillaStamina = new List<GameObject>();
        internal static Image[] ChevronFast;
        internal static Image[] ChevronSlow;
        private static int guardianUpdateFrame = -1;
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
        internal static BuffUI Buff;

        internal static Sprite DefaultFoodIcon;
        private static Sprite SolidBarSprite;

        internal static AssetBundle HudBundle;
        internal static AssetBundle FoodIconsBundle;
        private static readonly Dictionary<string, Sprite> EmbeddedFoodIcons = new Dictionary<string, Sprite>();
        private static bool embeddedIconsLoaded;

        internal static readonly Dictionary<string, Sprite> FoodIconOverrides =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        internal static GameObject VanillaGuardianRoot;
        internal static Image VanillaGuardianIcon;
        internal static Image VanillaGuardianTimeBar;
        internal static TextMeshProUGUI VanillaGuardianTimeText;
        internal static Text VanillaGuardianLegacyTimeText;
        internal static TextMeshProUGUI VanillaGuardianNameText;
        internal static Text VanillaGuardianLegacyNameText;

        internal const float TimerYOffset = -28f;
        internal const float BarTextRightOffset = -10f;

        internal static void RequestRebuild()
        {
            Reset();
            nextInitAttempt = 0f;
        }

        internal static void Reset()
        {
            Initialized = false;
            guardianUpdateFrame = -1;

            foreach (var go in HiddenVanillaStamina)
                if (go != null) go.SetActive(true);
            HiddenVanillaStamina.Clear();
            ChevronFast = null;
            ChevronSlow = null;

            if (Panel != null) UnityEngine.Object.Destroy(Panel);
            Panel = null;
            GuardianName = null;

            HpFastImage = null;
            HpSlowImage = null;
            HpText = null;
            HpFastValue = -1f;
            HpSlowValue = -1f;

            StaminaFastImage = null;
            StaminaSlowImage = null;
            StaminaText = null;
            StaminaFastValue = -1f;
            StaminaSlowValue = -1f;

            for (int i = 0; i < Slots.Length; i++)
                Slots[i] = null;

            Buff = null;

            VanillaGuardianRoot = null;
            VanillaGuardianIcon = null;
            VanillaGuardianTimeBar = null;
            VanillaGuardianTimeText = null;
            VanillaGuardianLegacyTimeText = null;
            VanillaGuardianNameText = null;
            VanillaGuardianLegacyNameText = null;
        }

        internal static void EnsureInit(Hud hud)
        {
            if (Initialized)
            {
                bool needsReset =
                    ActiveStyle != CurrentStyle ||
                    Panel == null ||
                    Buff == null ||
                    hud == null ||
                    hud.m_rootObject == null;

                if (!needsReset && Panel != null)
                {
                    try
                    {
                        bool panelBelongsToAnotherHud =
                            Panel.transform.parent != hud.m_rootObject.transform;

                        if (panelBelongsToAnotherHud)
                            needsReset = true;
                    }
                    catch
                    {
                        needsReset = true;
                    }
                }

                if (needsReset)
                    Reset();
            }

            if (Initialized || hud == null || hud.m_rootObject == null || Time.unscaledTime < nextInitAttempt) return;
            nextInitAttempt = Time.unscaledTime + 5f;

            try
            {
                if (HudBundle == null) HudBundle = LoadHudBundle();
                if (HudBundle == null)
                {
                    UnityEngine.Debug.LogError("[UIReforge] Failed to load the HUD bundle (embedded and prefabs/hudPrefab)");
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
                // GuiBar caches the prefab width in Awake and rewrites it in Update.
                // This panel drives Image.fillAmount itself, so the legacy drivers
                // must not fight the normalized layout on subsequent frames.
                foreach (var component in Panel.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component.GetType().Name == "GuiBar") component.enabled = false;
                FindReferences(Panel);
                if (HpFastImage == null || StaminaFastImage == null || HpText == null || StaminaText == null ||
                    Buff == null || Buff.Icon == null || Buff.Timer == null ||
                    Slots.Any(slot => slot == null || slot.Icon == null || slot.Timer == null))
                    throw new InvalidDataException("HUD prefab is missing required bars, labels or food slots");
                ActiveStyle = CurrentStyle;
                if (ActiveStyle == HudStyle.Chevron) ApplyChevronLayout();
                else ApplyReferenceLayout();

                CaptureVanillaGuardian(hud);
                HideVanillaGuardianVisuals();

                if (hud.m_healthPanel != null)
                    hud.m_healthPanel.gameObject.SetActive(false);

                if (ActiveStyle == HudStyle.Bars) HideVanillaStamina(hud);

                Initialized = true;
                UnityEngine.Debug.Log("[UIReforge] Custom HUD initialized 0.1.2, style " + ActiveStyle);
            }
            catch (Exception ex)
            {
                Reset();
                UnityEngine.Debug.LogError("[UIReforge] Init error: " + ex);
            }
        }

        // The panel prefab ships inside the DLL, so the mod works from any folder a
        // mod manager installs it to. A loose prefabs/hudPrefab next to the DLL (or
        // in BepInEx/plugins) is still accepted as a fallback for development builds.
        private static AssetBundle LoadHudBundle()
        {
            try
            {
                using (var stream = typeof(HudUpdatePlugin).Assembly.GetManifestResourceStream("UIReforge.Bundles.hudPrefab"))
                {
                    if (stream != null)
                    {
                        using (var bytes = new MemoryStream())
                        {
                            stream.CopyTo(bytes);
                            var bundle = AssetBundle.LoadFromMemory(bytes.ToArray());
                            if (bundle != null) return bundle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[UIReforge] Embedded HUD bundle failed: " + ex.Message);
            }
            string path = FindPluginFile("prefabs", "hudPrefab");
            return File.Exists(path) ? AssetBundle.LoadFromFile(path) : null;
        }

        private static string FindPluginFile(string folder, string file)
        {
            string own = Path.Combine(Path.GetDirectoryName(typeof(HudUpdatePlugin).Assembly.Location) ?? Paths.PluginPath, folder, file);
            return File.Exists(own) ? own : Path.Combine(Paths.PluginPath, folder, file);
        }

        internal static void LoadFoodIconsBundle()
        {
            try
            {
                FoodIconOverrides.Clear();

                string foodBundlePath = FindPluginFile("prefabs", "food_icons");
                if (!File.Exists(foodBundlePath))
                    return;

                if (FoodIconsBundle == null) FoodIconsBundle = AssetBundle.LoadFromFile(foodBundlePath);
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

                UnityEngine.Debug.Log($"[UIReforge] Loaded food icon overrides: {FoodIconOverrides.Count}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] LoadFoodIconsBundle error: " + ex);
            }
            finally
            {
                LoadEmbeddedFoodIcons();
            }
        }

        // Icons are looked up by file name (without .png), highest priority first:
        //   1. BepInEx/config/HUD-Update/Icons  - personal replacements, survive mod updates;
        //   2. Icons/ next to HUD-Update.dll    - the shipped copies, free to edit;
        //   3. the copies embedded in the DLL   - used when a file is missing or broken.
        // A file with a new name adds an icon: a food prefab name (CookedMeat.png),
        // a guardian power (GP_Moder.png) or a boss name.
        internal static readonly string[] IconFolders =
        {
            Path.Combine(Path.Combine(Paths.ConfigPath, "HUD-Update"), "Icons"),
            Path.Combine(Path.GetDirectoryName(typeof(HudUpdatePlugin).Assembly.Location) ?? Paths.PluginPath, "Icons"),
        };

        // UI pieces that are not food and need no glow outline.
        private static readonly HashSet<string> NonFoodIcons = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "HealthIcon", "StaminaIcon", "chevron_shape", "chevron_outline", "Eikthyr", "TheElder", "Bonemass" };

        private static readonly List<UnityEngine.Object> LoadedIconObjects = new List<UnityEngine.Object>();

        private static void LoadEmbeddedFoodIcons()
        {
            // Keep decoded textures across world changes; ReloadIcons() clears them.
            if (!embeddedIconsLoaded)
            {
                embeddedIconsLoaded = true;
                var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // key -> file path or null
                const string prefix = "UIReforge.Icons.";
                var assembly = typeof(HudUpdatePlugin).Assembly;
                foreach (string resource in assembly.GetManifestResourceNames()
                    .Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                    sources[resource.Substring(prefix.Length, resource.Length - prefix.Length - 4)] = null;
                for (int i = IconFolders.Length - 1; i >= 0; i--)
                {
                    try
                    {
                        if (!Directory.Exists(IconFolders[i])) continue;
                        foreach (string file in Directory.GetFiles(IconFolders[i], "*.png"))
                            sources[Path.GetFileNameWithoutExtension(file)] = file;
                    }
                    catch (Exception ex) { UnityEngine.Debug.LogWarning("[UIReforge] Could not read " + IconFolders[i] + ": " + ex.Message); }
                }

                int fromDisk = 0;
                foreach (var source in sources)
                {
                    string key = source.Key;
                    Sprite sprite = null;
                    if (source.Value != null)
                    {
                        try { sprite = CreateIconSprite(key, File.ReadAllBytes(source.Value)); fromDisk++; }
                        catch (Exception ex) { UnityEngine.Debug.LogWarning("[UIReforge] Icon " + source.Value + " skipped: " + ex.Message); }
                    }
                    if (sprite == null)
                    {
                        using (var input = assembly.GetManifestResourceStream(prefix + key + ".png"))
                        {
                            if (input == null) continue;
                            using (var bytes = new MemoryStream())
                            {
                                input.CopyTo(bytes);
                                try { sprite = CreateIconSprite(key, bytes.ToArray()); }
                                catch (Exception ex) { UnityEngine.Debug.LogWarning("[UIReforge] Could not load embedded icon " + key + ": " + ex.Message); }
                            }
                        }
                    }
                    if (sprite != null) EmbeddedFoodIcons[key] = sprite;
                }
                UnityEngine.Debug.Log("[UIReforge] Icons: " + EmbeddedFoodIcons.Count + " (" + fromDisk + " from Icons folders)");
            }
            foreach (var icon in EmbeddedFoodIcons) FoodIconOverrides[icon.Key] = icon.Value;
        }

        private static Sprite CreateIconSprite(string key, byte[] png)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, png, false))
                    throw new InvalidDataException("not a valid PNG");
                texture.name = key;
                texture.wrapMode = TextureWrapMode.Clamp;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                sprite.name = key;
                if (!NonFoodIcons.Contains(key) && !key.StartsWith("GP_", StringComparison.OrdinalIgnoreCase))
                {
                    var glow = BuildGlowSprite(texture, key);
                    GlowSprites[sprite] = glow;
                    LoadedIconObjects.Add(glow);
                    LoadedIconObjects.Add(glow.texture);
                }
                texture.Apply(false, true);   // glow is built, free the CPU copy
                UnityEngine.Object.DontDestroyOnLoad(texture);
                UnityEngine.Object.DontDestroyOnLoad(sprite);
                LoadedIconObjects.Add(sprite);
                LoadedIconObjects.Add(texture);
                return sprite;
            }
            catch
            {
                UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        // Called when a PNG in one of the Icons folders changes while the game runs.
        internal static void ReloadIcons()
        {
            RequestRebuild();
            foreach (var obj in LoadedIconObjects)
                if (obj != null) UnityEngine.Object.Destroy(obj);
            LoadedIconObjects.Clear();
            GlowSprites.Clear();
            EmbeddedFoodIcons.Clear();
            FoodIconOverrides.Clear();
            embeddedIconsLoaded = false;
            UnityEngine.Debug.Log("[UIReforge] Icons folder changed, reloading icons");
        }

        internal static string IconFolderStamp()
        {
            var stamp = new System.Text.StringBuilder();
            foreach (string folder in IconFolders)
            {
                try
                {
                    if (!Directory.Exists(folder)) continue;
                    foreach (string file in Directory.GetFiles(folder, "*.png"))
                        stamp.Append(file).Append(File.GetLastWriteTimeUtc(file).Ticks).Append(';');
                }
                catch { }
            }
            return stamp.ToString();
        }

        internal static void FindReferences(GameObject root)
        {
            FindBarGroup(root.transform.Find("Health"), out HpFastImage, out HpSlowImage, out HpText);
            FindBarGroup(root.transform.Find("Stamina"), out StaminaFastImage, out StaminaSlowImage, out StaminaText);

            Transform buffTr = root.transform.Find("Buff");
            if (buffTr != null)
            {
                Buff = new BuffUI
                {
                    Root = buffTr.gameObject,
                    Icon = buffTr.Find("buffIcon")?.GetComponent<Image>(),
                    Timer = buffTr.Find("time")?.GetComponent<TextMeshProUGUI>()
                };

                if (Buff.Root != null)
                    Buff.Root.SetActive(false);

                if (Buff.Icon != null)
                {
                    Buff.Icon.color = Color.white;
                    Buff.Icon.preserveAspect = true;
                    Buff.Icon.raycastTarget = false;
                }

                if (Buff.Timer != null)
                {
                    SetupDiamondTimerText(Buff.Timer);
                    Buff.Timer.transform.SetAsLastSibling();
                }
            }
            else
            {
                Buff = null;
            }

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
                if (DefaultFoodIcon == null && slot.VanillaIcon != null) DefaultFoodIcon = slot.VanillaIcon.sprite;
                if (slot.VanillaIcon != null)
                    slot.VanillaIcon.enabled = false;

                var glowGO = new GameObject("UIReforgeGlow", typeof(RectTransform), typeof(Image));
                glowGO.transform.SetParent(slotTr, false);
                slot.Glow = glowGO.GetComponent<Image>();
                slot.Glow.raycastTarget = false;
                slot.Glow.enabled = false;

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
                    SetupDiamondTimerText(slot.Timer);
                    slot.Timer.transform.SetAsLastSibling();
                }

                Slots[i] = slot;
            }
        }

        // Keep the imported images and masks; only normalize their RectTransforms.
        internal static void Place(RectTransform rt, float x, float y, float width, float height,
            bool fromBottomLeft = false, float rotation = 0f)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = fromBottomLeft ? Vector2.zero : new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0, 0, rotation);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        // Reference geometry (canvas units, panel bottom-left origin).
        internal static readonly Color PanelDark = new Color(.105f, .105f, .085f, .93f);
        internal static readonly Color HpColor = new Color(.66f, .12f, .12f, 1f);
        internal static readonly Color HpTrailColor = new Color(.93f, .78f, .72f, 1f);
        internal static readonly Color StaminaColor = new Color(.84f, .65f, .43f, 1f);
        internal static readonly Color StaminaTrailColor = new Color(.97f, .9f, .78f, 1f);
        private const float BarLeft = 238f, BarWidth = 266f, BarHeight = 27f, PlateWidth = 34f;
        private const float HpBarY = 122f, StaminaBarY = 88f;

        internal static HudStyle CurrentStyle =>
            HudUpdatePlugin.Style != null ? HudUpdatePlugin.Style.Value : HudStyle.Bars;

        private static void BuildGuardian(RectTransform root, Vector2 center, Vector2 iconPos, Vector2 iconSize,
            float nameY, bool outlinedName)
        {
            if (Buff == null || Buff.Root == null) return;
            var background = Buff.Root.GetComponent<RectTransform>();
            var content = new GameObject("GuardianContent", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(root, false);
            Place(content, center.x, center.y, 142, 142, true);
            background.SetParent(content, false);
            Place(background, 0, 0, 100, 100, false, 45);
            background.gameObject.SetActive(true);
            TintDiamond(background);
            if (Buff.Icon != null)
            {
                Buff.Icon.transform.SetParent(content, false);
                Place(Buff.Icon.rectTransform, iconPos.x, iconPos.y, iconSize.x, iconSize.y);
            }
            if (Buff.Timer != null)
            {
                Buff.Timer.transform.SetParent(content, false);
                Place(Buff.Timer.rectTransform, 0, nameY - 20f, 130, 18);
                StyleText(Buff.Timer, false);
                Buff.Timer.fontSize = 13;
                Buff.Timer.alignment = TextAlignmentOptions.Center;
                GuardianName = UnityEngine.Object.Instantiate(Buff.Timer, content);
                GuardianName.name = "GuardianName";
                Place(GuardianName.rectTransform, 0, nameY, 150, 24);
                StyleText(GuardianName, true);
                GuardianName.fontSize = outlinedName ? 18 : 17;
                if (outlinedName) Outline(GuardianName, .16f);
                if (outlinedName) Outline(Buff.Timer, .14f);
                GuardianName.text = "";
            }
            Buff.Root = content.gameObject;
        }

        // Food timers get a crisp dark rim made of 8 offset copies instead of the
        // SDF outline: the game font has little atlas padding, so a shader outline
        // on 11-14 pt text renders soft/blurry.
        private const float FoodTimerRim = 1f;
        private static readonly Color FoodTimerRimColor = new Color(0f, 0f, 0f, .85f);

        private static void BuildTimerShadows()
        {
            foreach (var slot in Slots)
            {
                if (slot == null || slot.Timer == null) continue;
                var timer = slot.Timer;
                if (timer.font != null && timer.font.material != null) timer.fontSharedMaterial = timer.font.material;
                var copies = new List<TextMeshProUGUI>();
                int index = timer.transform.GetSiblingIndex();
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var copy = UnityEngine.Object.Instantiate(timer, timer.transform.parent);
                    copy.name = "TimerRim";
                    foreach (Transform child in copy.transform) UnityEngine.Object.Destroy(child.gameObject);
                    copy.fontSharedMaterial = timer.fontSharedMaterial;
                    copy.color = FoodTimerRimColor;   // vertex colour only: all copies share one material
                    copy.raycastTarget = false;
                    copy.rectTransform.anchoredPosition = timer.rectTransform.anchoredPosition + new Vector2(dx, dy) * FoodTimerRim;
                    copy.transform.SetSiblingIndex(index);
                    copies.Add(copy);
                }
                timer.transform.SetAsLastSibling();
                slot.TimerShadow = copies.ToArray();
            }
        }

        internal static void SyncTimerShadow(FoodSlotUI slot)
        {
            if (slot?.TimerShadow == null || slot.Timer == null) return;
            foreach (var copy in slot.TimerShadow)
            {
                if (copy == null) continue;
                if (copy.text != slot.Timer.text) copy.text = slot.Timer.text;
                copy.enabled = slot.Timer.enabled;
            }
        }

        private static void Outline(TextMeshProUGUI text, float width)
        {
            // TMP draws the outline half inside the glyph; dilate the face by the
            // same amount so letters keep their bold weight and stay white.
            text.outlineColor = new Color32(12, 10, 8, 255);
            text.outlineWidth = width;
            try { text.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, width); }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[UIReforge] Face dilate failed: " + ex.Message); }
        }

        internal static void ApplyReferenceLayout()
        {
            var root = Panel.GetComponent<RectTransform>();
            Place(root, 32, 26, 550, 200, true);
            root.pivot = Vector2.zero;
            var darken = Panel.transform.Find("darken");
            if (darken != null) darken.gameObject.SetActive(false);

            BuildGuardian(root, new Vector2(80, 105), new Vector2(2, 12), new Vector2(98, 90), -52f, false);

            float[] x = { 158, 209, 158 };
            float[] y = { 153, 105, 57 };
            for (int i = 0; i < Slots.Length; i++)
            {
                var slot = Slots[i];
                if (slot == null) continue;
                Place(slot.Root.GetComponent<RectTransform>(), x[i], y[i], 62.5f, 62.5f, true);
                var substrate = slot.Root.transform.Find("substrate") as RectTransform;
                Place(substrate, 0, 0, 62.5f, 62.5f, false, 45);
                TintDiamond(substrate);
                if (slot.Icon != null) Place(slot.Icon.rectTransform, 0, 2, 28, 28);
                PlaceGlow(slot);
                SetupDiamondTimerText(slot.Timer);
            }

            BuildBar("Health", HpBarY, false, HpColor, HpTrailColor, ref HpFastImage, ref HpSlowImage, ref HpText);
            BuildBar("Stamina", StaminaBarY, true, StaminaColor, StaminaTrailColor, ref StaminaFastImage, ref StaminaSlowImage, ref StaminaText);
            LayoutResourceIcon("HealthIcon", HpBarY);
            LayoutResourceIcon("StaminaIcon", StaminaBarY);
            BuildTimerShadows();
            foreach (Graphic graphic in Panel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        // ---- Chevron style -------------------------------------------------
        // Geometry mirrors IconSource/chevron.py (canvas units, y up, relative to
        // the centre C of the 2x2 food grid). Guardian sits at C - (77, 0).
        internal static readonly Color ChevronRed = new Color(.678f, .086f, .086f, 1f);   // Figma #AD1616
        internal static readonly Color ChevronTrail = new Color(.95f, .8f, .72f, 1f);
        internal static readonly Color ChevronTrack = new Color(.07f, .05f, .04f, .55f);
        internal static readonly Color ChevronOutline = new Color(0f, 0f, 0f, 1f);
        internal static readonly Color ChevronCell = new Color(0f, 0f, 0f, .72f);          // Figma: black 80% fill x 90% layer
        internal static readonly Color FoodBackplate = new Color(.66f, .64f, .30f, .55f);
        // Bounds of the Figma "Subtract" node (149x154 px, 1 px = 4/3 unit) around C.
        private const float ChevX0 = -79.71f, ChevX1 = 118.96f, ChevY0 = -87.96f, ChevY1 = 117.38f;
        private const float ChevCorner = 14f, ChevBand = 44f;
        // Centre line of the bar: tail (under the label) -> top -> right -> bottom end.
        private static readonly Vector2[] ChevPath =
        {
            new Vector2(-85.04f, 86.71f), new Vector2(-25.04f, 86.71f), new Vector2(-1.04f, 108.04f),
            new Vector2(107.89f, 0.31f), new Vector2(16.29f, -95.96f)
        };

        internal static void ApplyChevronLayout()
        {
            var root = Panel.GetComponent<RectTransform>();
            Place(root, 32, 26, 550, 200, true);
            root.pivot = Vector2.zero;
            var darken = Panel.transform.Find("darken");
            if (darken != null) darken.gameObject.SetActive(false);

            var guardian = new Vector2(80, 105);
            var c = guardian + new Vector2(77, 0);

            // No backplate: the gaps between the food diamonds stay see-through.
            // Figma "Group 117": 45 px cells, guardian 75 px -> 100 units.
            Vector2[] food = { c + new Vector2(-1.28f, 45.62f), c + new Vector2(46.72f, 0.28f), c + new Vector2(-1.28f, -46.38f) };
            for (int i = 0; i < Slots.Length; i++)
            {
                var slot = Slots[i];
                if (slot == null) continue;
                Place(slot.Root.GetComponent<RectTransform>(), food[i].x, food[i].y, 60f, 60f, true);
                var substrate = slot.Root.transform.Find("substrate") as RectTransform;
                Place(substrate, 0, 0, 60f, 60f, false, 45);
                TintDiamond(substrate);
                var cell = substrate != null ? substrate.GetComponent<Image>() : null;
                if (cell != null) cell.color = ChevronCell;
                if (slot.Icon != null) Place(slot.Icon.rectTransform, 0, 0, 33, 33);
                PlaceGlow(slot);
                SetupDiamondTimerText(slot.Timer);
                if (slot.Timer != null)
                {
                    // Figma: Inter Regular 8 px, offset (+7.7, -11) px from the cell centre.
                    slot.Timer.rectTransform.anchoredPosition = new Vector2(10.2f, -13.5f);
                    StyleText(slot.Timer, false);
                    slot.Timer.fontSize = 11;
                }
            }

            BuildGuardian(root, guardian, new Vector2(2, 12), new Vector2(98, 90), -51.3f, false);
            if (GuardianName != null) GuardianName.fontSize = 16;   // Figma: Inter Bold 12 px, no stroke
            var guardianBg = Buff != null && Buff.Root != null ? Buff.Root.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(img => Buff.Icon == null || img != Buff.Icon) : null;
            if (guardianBg != null) guardianBg.color = ChevronCell;
            BuildChevron(root, c);

            foreach (string name in new[] { "Health", "Stamina", "HealthIcon", "StaminaIcon" })
            {
                var t = Panel.transform.Find(name);
                if (t != null && t.GetComponent<TextMeshProUGUI>() == null) t.gameObject.SetActive(false);
            }
            StaminaFastImage = StaminaSlowImage = null;
            StaminaText = null;
            BuildTimerShadows();
            foreach (Graphic graphic in Panel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private static void BuildChevron(RectTransform root, Vector2 c)
        {
            FoodIconOverrides.TryGetValue("chevron_shape", out var shape);
            FoodIconOverrides.TryGetValue("chevron_outline", out var outline);
            if (shape == null) throw new InvalidDataException("chevron_shape.png is not embedded");

            var mid = new Vector2((ChevX0 + ChevX1) / 2f, (ChevY0 + ChevY1) / 2f);
            var group = NewRect("HpChevron", root);
            Place(group, c.x + mid.x, c.y + mid.y, ChevX1 - ChevX0, ChevY1 - ChevY0, true);

            var track = NewRect("track", group, typeof(Image)).GetComponent<Image>();
            Stretch(track.rectTransform);
            track.sprite = shape;
            track.color = ChevronTrack;

            var maskImage = NewRect("fill", group, typeof(Image), typeof(Mask)).GetComponent<Image>();
            Stretch(maskImage.rectTransform);
            maskImage.sprite = shape;
            maskImage.GetComponent<Mask>().showMaskGraphic = false;

            ChevronSlow = BuildChevronSegments("slow", maskImage.transform, mid, ChevronTrail);
            ChevronFast = BuildChevronSegments("fast", maskImage.transform, mid, ChevronRed);
            HpFastImage = HpSlowImage = null;

            if (outline != null)
            {
                var edge = NewRect("outline", group, typeof(Image)).GetComponent<Image>();
                Stretch(edge.rectTransform);
                edge.sprite = outline;
                edge.color = ChevronOutline;
            }

            if (HpText != null)
            {
                HpText.transform.SetParent(root, false);
                var rt = HpText.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0f, .5f);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.anchoredPosition = new Vector2(c.x - 79.71f, c.y + 102f);
                rt.sizeDelta = new Vector2(90f, 22f);
                StyleText(HpText, true);   // Figma: Inter Bold 12 px, stroke hidden
                HpText.fontSize = 16;
                HpText.alignment = TextAlignmentOptions.Left;
                HpText.textWrappingMode = TextWrappingModes.NoWrap;
                HpText.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static Image[] BuildChevronSegments(string name, Transform parent, Vector2 mid, Color color)
        {
            var result = new Image[ChevPath.Length - 1];
            for (int i = 0; i < result.Length; i++)
            {
                var image = NewRect(name + i, parent, typeof(Image)).GetComponent<Image>();
                var rt = image.rectTransform;
                Vector2 dir = ChevPath[i + 1] - ChevPath[i];
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                rt.pivot = new Vector2(0f, .5f);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                rt.anchoredPosition = ChevPath[i] - mid;
                rt.sizeDelta = new Vector2(0f, ChevBand);
                image.sprite = SolidSprite();
                image.color = color;
                image.enabled = false;
                result[i] = image;
            }
            return result;
        }

        // Fills the chevron from the tail (next to the HP label) towards the bottom end.
        internal static void SetChevronFill(Image[] segments, float value)
        {
            if (segments == null) return;
            var mid = new Vector2((ChevX0 + ChevX1) / 2f, (ChevY0 + ChevY1) / 2f);
            float total = 0f;
            for (int i = 0; i < segments.Length; i++) total += (ChevPath[i + 1] - ChevPath[i]).magnitude;
            float remaining = Mathf.Clamp01(value) * total;
            for (int i = 0; i < segments.Length; i++)
            {
                var image = segments[i];
                if (image == null) continue;
                Vector2 delta = ChevPath[i + 1] - ChevPath[i];
                float length = delta.magnitude;
                float filled = Mathf.Clamp(remaining, 0f, length);
                remaining -= length;
                image.enabled = filled > .05f;
                if (!image.enabled) continue;
                Vector2 dir = delta / length;
                float back = i > 0 ? ChevCorner : 0f;
                float forward = filled >= length - .05f && i < segments.Length - 1 ? ChevCorner : 0f;
                var rt = image.rectTransform;
                rt.anchoredPosition = ChevPath[i] - mid - dir * back;
                rt.sizeDelta = new Vector2(back + filled + forward, ChevBand);
            }
        }

        private static void TintDiamond(Transform diamond)
        {
            if (diamond == null) return;
            var image = diamond.GetComponent<Image>();
            if (image != null) image.color = PanelDark;
        }

        private static RectTransform NewRect(string name, Transform parent, params Type[] components)
        {
            var types = new List<Type> { typeof(RectTransform) };
            types.AddRange(components);
            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void PlaceLeft(RectTransform rt, float left, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, .5f);
            rt.pivot = new Vector2(0f, .5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = new Vector2(left, 0f);
            rt.sizeDelta = new Vector2(width, height);
        }

        // Replaces the prefab bar with a procedural one: a 45-degree cut on the
        // left that runs parallel to the middle food diamond, a solid right end
        // and a dark value plate inside the bar.
        private static void BuildBar(string name, float y, bool cutFromBottom, Color fill, Color trail,
            ref Image fast, ref Image slow, ref TextMeshProUGUI text)
        {
            var old = Panel.transform.Find(name);
            var group = NewRect("UIReforge" + name, Panel.transform);
            Place(group, BarLeft + BarWidth / 2f, y, BarWidth, BarHeight, true);

            float fillWidth = BarWidth - PlateWidth;
            Sprite cut = CreateCutSprite(fillWidth, BarHeight, cutFromBottom);

            var track = NewRect("track", group, typeof(Image)).GetComponent<Image>();
            PlaceLeft(track.rectTransform, 0, fillWidth, BarHeight);
            track.sprite = cut;
            track.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, .75f);

            slow = NewFilled("slow", group, cut, trail, fillWidth);
            fast = NewFilled("fast", group, cut, fill, fillWidth);

            var plate = NewRect("plate", group, typeof(Image)).GetComponent<Image>();
            PlaceLeft(plate.rectTransform, fillWidth, PlateWidth, BarHeight);
            plate.sprite = SolidSprite();
            plate.color = PanelDark;

            if (text != null)
            {
                text.transform.SetParent(plate.transform, false);
                var rt = text.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(.5f, .5f);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.offsetMin = new Vector2(1f, 0f);
                rt.offsetMax = new Vector2(-1f, 0f);
                StyleText(text, true);
                text.fontSize = 15;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
            }

            if (old != null) old.gameObject.SetActive(false);
        }

        private static Image NewFilled(string name, Transform parent, Sprite sprite, Color color, float width)
        {
            var image = NewRect(name, parent, typeof(Image)).GetComponent<Image>();
            PlaceLeft(image.rectTransform, 0, width, BarHeight);
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            image.preserveAspect = false;
            return image;
        }

        private static Sprite solidSprite;
        private static Sprite SolidSprite()
        {
            if (solidSprite == null)
            {
                solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
                UnityEngine.Object.DontDestroyOnLoad(solidSprite);
            }
            return solidSprite;
        }

        private static readonly Dictionary<string, Sprite> cutSprites = new Dictionary<string, Sprite>();

        // White parallelogram-left / square-right mask, rendered at 4x for clean edges.
        private static Sprite CreateCutSprite(float width, float height, bool cutFromBottom)
        {
            string key = width + "x" + height + (cutFromBottom ? "b" : "t");
            if (cutSprites.TryGetValue(key, out var cached) && cached != null) return cached;

            const int scale = 4;
            int w = Mathf.RoundToInt(width * scale), h = Mathf.RoundToInt(height * scale);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[w * h];
            for (int py = 0; py < h; py++)
            {
                // cutFromBottom: bottom edge starts furthest left ("/"), otherwise the top does ("\").
                float edge = cutFromBottom ? py + .5f : h - py - .5f;
                for (int px = 0; px < w; px++)
                {
                    float a = Mathf.Clamp01(px + .5f - edge + .5f);
                    pixels[py * w + px] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
            sprite.name = "UIReforgeBarCut_" + key;
            UnityEngine.Object.DontDestroyOnLoad(texture);
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            cutSprites[key] = sprite;
            return sprite;
        }

        internal static void SetupDiamondTimerText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(12f, -13f);
            rt.sizeDelta = new Vector2(46f, 20f);

            StyleText(text, true);
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        private static void LayoutResourceIcon(string name, float y)
        {
            var existing = Panel.transform.Find(name);
            var icon = existing != null ? existing.GetComponent<Image>() : null;
            if (icon == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(Panel.transform, false);
                icon = go.GetComponent<Image>();
            }
            if (FoodIconOverrides.TryGetValue(name, out var sprite)) icon.sprite = sprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            Place(icon.rectTransform, BarLeft + BarWidth + 10f, y, 12f, 14f, true);
            icon.transform.SetAsLastSibling();
        }

        private static void StyleText(TextMeshProUGUI text, bool bold)
        {
            text.enableAutoSizing = false;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = Color.white;
            text.faceColor = Color.white;
            text.outlineWidth = 0f;
            text.enableVertexGradient = false;
            text.characterSpacing = 0;
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

            Transform maskTr = groupRoot.Find("Mask");
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

            if (text != null)
            {
                RectTransform textRt = text.rectTransform;

                if (maskTr is RectTransform maskRt)
                    textRt.SetParent(maskRt, true);

                textRt.anchorMin = new Vector2(1f, 0.5f);
                textRt.anchorMax = new Vector2(1f, 0.5f);
                textRt.pivot = new Vector2(1f, 0.5f);
                textRt.anchoredPosition = new Vector2(BarTextRightOffset, 0f);
                textRt.sizeDelta = new Vector2(80f, textRt.sizeDelta.y);

                text.alignment = TextAlignmentOptions.Right;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.transform.SetAsLastSibling();
            }
        }

        internal static void SetupFilledBar(Image img)
        {
            if (img == null) return;

            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;

            // The imported sprite already has a cut edge. A solid fill lets the
            // parent stencil provide the single, consistent diagonal at any width.
            if (SolidBarSprite == null)
                SolidBarSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
            img.sprite = SolidBarSprite;
            img.preserveAspect = false;
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
                        if (tr.gameObject.activeSelf) HiddenVanillaStamina.Add(tr.gameObject);
                        tr.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        internal static void CaptureVanillaGuardian(Hud hud)
        {
            try
            {
                if (hud?.m_rootObject == null)
                    return;

                var guardianTr = FindDeepChild(hud.m_rootObject.transform, "GuardianPower");
                if (guardianTr == null)
                {
                    UnityEngine.Debug.LogWarning("[UIReforge] Vanilla GuardianPower not found");
                    return;
                }

                VanillaGuardianRoot = guardianTr.gameObject;

                VanillaGuardianIcon = guardianTr.Find("Icon")?.GetComponent<Image>();

                Transform timeBarTr = guardianTr.Find("TimeBar");
                if (timeBarTr != null)
                {
                    VanillaGuardianTimeBar = timeBarTr.GetComponent<Image>();
                    if (VanillaGuardianTimeBar == null)
                        VanillaGuardianTimeBar = timeBarTr.GetComponentInChildren<Image>(true);
                }

                VanillaGuardianTimeText = guardianTr.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
                if (VanillaGuardianTimeText == null)
                    VanillaGuardianTimeText = guardianTr.GetComponentInChildren<TextMeshProUGUI>(true);

                VanillaGuardianLegacyTimeText = guardianTr.Find("TimeText")?.GetComponent<Text>();
                if (VanillaGuardianLegacyTimeText == null)
                    VanillaGuardianLegacyTimeText = guardianTr.GetComponentInChildren<Text>(true);

                VanillaGuardianNameText = guardianTr.Find("Name")?.GetComponent<TextMeshProUGUI>();
                if (VanillaGuardianNameText == null)
                    VanillaGuardianNameText = guardianTr.GetComponentsInChildren<TextMeshProUGUI>(true)
                        .FirstOrDefault(x => x.name == "Name");

                VanillaGuardianLegacyNameText = guardianTr.Find("Name")?.GetComponent<Text>();
                if (VanillaGuardianLegacyNameText == null)
                    VanillaGuardianLegacyNameText = guardianTr.GetComponentsInChildren<Text>(true)
                        .FirstOrDefault(x => x.name == "Name");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] CaptureVanillaGuardian error: " + ex);
            }
        }

        internal static void HideVanillaGuardianVisuals()
        {
            try
            {
                if (VanillaGuardianRoot == null)
                    return;

                foreach (var g in VanillaGuardianRoot.GetComponentsInChildren<Graphic>(true))
                {
                    g.enabled = false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] HideVanillaGuardianVisuals error: " + ex);
            }
        }

        internal static void RefreshGuardian(Hud hud)
        {
            if (guardianUpdateFrame == Time.frameCount) return;
            guardianUpdateFrame = Time.frameCount;
            try
            {
                if (hud == null)
                    return;

                if (VanillaGuardianRoot == null)
                    CaptureVanillaGuardian(hud);

                HideVanillaGuardianVisuals();
                SyncGuardianFromVanilla();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] RefreshGuardian error: " + ex);
            }
        }

        internal static void SyncGuardianFromVanilla()
        {
            try
            {
                if (Buff == null || Buff.Root == null)
                    return;

                if (VanillaGuardianRoot == null)
                {
                    Buff.Root.SetActive(false);
                    return;
                }

                string timeText = string.Empty;
                if (VanillaGuardianTimeText != null)
                    timeText = VanillaGuardianTimeText.text;
                else if (VanillaGuardianLegacyTimeText != null)
                    timeText = VanillaGuardianLegacyTimeText.text;

                string guardianName = string.Empty;
                if (VanillaGuardianNameText != null)
                    guardianName = VanillaGuardianNameText.text;
                else if (VanillaGuardianLegacyNameText != null)
                    guardianName = VanillaGuardianLegacyNameText.text;

                if (GuardianName != null) GuardianName.text = guardianName.ToUpperInvariant();

                Sprite vanillaSprite = VanillaGuardianIcon != null ? VanillaGuardianIcon.sprite : null;
                Sprite finalSprite = ResolveGuardianIcon(guardianName, vanillaSprite);

                bool shouldShow = finalSprite != null || !string.IsNullOrWhiteSpace(timeText);

                Buff.Root.SetActive(shouldShow);

                if (!shouldShow)
                    return;

                if (Buff.Icon != null)
                {
                    Buff.Icon.sprite = finalSprite;
                    Buff.Icon.enabled = true;
                    Buff.Icon.gameObject.SetActive(true);
                    Buff.Icon.color = Color.white;
                    Buff.Icon.preserveAspect = true;
                    Buff.Icon.transform.localScale = Vector3.one;
                    Buff.Icon.transform.SetAsLastSibling();
                }

                if (Buff.Timer != null)
                {
                    float cooldown = 0f;
                    if (Player.m_localPlayer != null) Player.m_localPlayer.GetGuardianPowerHUD(out StatusEffect effect, out cooldown);
                    Buff.Timer.text = timeText;
                    Buff.Timer.enabled = cooldown > 0f;
                    Buff.Timer.transform.SetAsLastSibling();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] SyncGuardianFromVanilla error: " + ex);
            }
        }

        // Embedded guardian emblems (white knotwork, Icons/<name>.png). Other powers keep the game icon.
        private static readonly Dictionary<string, string> GuardianEmblems = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "GP_Eikthyr", "Eikthyr" },
            { "GP_TheElder", "TheElder" },
            { "GP_Bonemass", "Bonemass" },
        };

        internal static Sprite ResolveGuardianIcon(string guardianName, Sprite vanillaSprite)
        {
            try
            {
                // Guardian power prefab (GP_*) -> embedded emblem. Works in every game language.
                string power = Player.m_localPlayer != null ? Player.m_localPlayer.GetGuardianPowerName() : null;
                if (!string.IsNullOrEmpty(power) && GuardianEmblems.TryGetValue(power, out var emblemKey) &&
                    FoodIconOverrides.TryGetValue(emblemKey, out var emblem))
                    return emblem;
                // A player's own emblem named after the power, e.g. Icons/GP_Moder.png.
                if (!string.IsNullOrEmpty(power) && FoodIconOverrides.TryGetValue(power, out var byPower))
                    return byPower;

                if (!string.IsNullOrWhiteSpace(guardianName))
                {
                    string normalized = guardianName.ToLowerInvariant().Replace("$", "").Replace("guardian_", "");
                    foreach (var pair in GuardianEmblems)
                    {
                        if (normalized.Contains(pair.Value.ToLowerInvariant().Replace("the", "")) &&
                            FoodIconOverrides.TryGetValue(pair.Value, out var byName))
                            return byName;
                    }

                    if (FoodIconOverrides.TryGetValue(guardianName, out var directByName))
                        return directByName;
                }

                if (vanillaSprite != null &&
                    FoodIconOverrides.TryGetValue(vanillaSprite.name, out var bySpriteName))
                {
                    return bySpriteName;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[UIReforge] ResolveGuardianIcon error: " + ex);
            }

            return vanillaSprite;
        }

        internal static Transform FindDeepChild(Transform parent, string exactName)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, exactName, StringComparison.Ordinal))
                    return child;

                Transform nested = FindDeepChild(child, exactName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        internal static Image FindVanillaFoodIconImage(Transform slotTr)
        {
            foreach (var img in slotTr.GetComponentsInChildren<Image>(true))
            {
                if (img.name == "UIReforgeIcon" || img.name == "UIReforgeGlow")
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

        // ---- Food stat colouring -------------------------------------------
        internal enum FoodStat { Neutral, Health, Stamina, Eitr }
        private static readonly Dictionary<Sprite, Sprite> GlowSprites = new Dictionary<Sprite, Sprite>();
        private static Sprite radialGlow;
        private const float GlowScale = 1.7f;

        internal static FoodStat DominantStat(ItemDrop.ItemData item)
        {
            var shared = item?.m_shared;
            if (shared == null) return FoodStat.Neutral;
            float[] values = { shared.m_food, shared.m_foodStamina, shared.m_foodEitr };
            int best = 0;
            for (int i = 1; i < values.Length; i++) if (values[i] > values[best]) best = i;
            float second = 0f;
            for (int i = 0; i < values.Length; i++) if (i != best) second = Mathf.Max(second, values[i]);
            // A stat counts as dominant only when it is clearly ahead (30 %).
            if (values[best] <= 0f || values[best] < second * 1.3f) return FoodStat.Neutral;
            return best == 0 ? FoodStat.Health : best == 1 ? FoodStat.Stamina : FoodStat.Eitr;
        }

        internal static Color GlowColor(FoodStat stat)
        {
            switch (stat)
            {
                case FoodStat.Health: return new Color(1f, .22f, .18f, 1f);
                case FoodStat.Stamina: return new Color(1f, .78f, .22f, 1f);
                case FoodStat.Eitr: return new Color(.32f, .58f, 1f, 1f);
                default: return new Color(1f, 1f, 1f, .5f);
            }
        }

        internal static Color TintColor(FoodStat stat)
        {
            switch (stat)
            {
                case FoodStat.Health: return new Color(1f, .64f, .6f, 1f);
                case FoodStat.Stamina: return new Color(1f, .88f, .56f, 1f);
                case FoodStat.Eitr: return new Color(.68f, .8f, 1f, 1f);
                default: return Color.white;
            }
        }

        // The mod's own flat icons (embedded or from the food_icons bundle).
        internal static bool IsStyledIcon(Sprite sprite)
        {
            return sprite != null && (GlowSprites.ContainsKey(sprite) || FoodIconOverrides.ContainsValue(sprite));
        }

        private static void PlaceGlow(FoodSlotUI slot)
        {
            if (slot?.Glow == null || slot.Icon == null) return;
            var icon = slot.Icon.rectTransform;
            Place(slot.Glow.rectTransform, icon.anchoredPosition.x, icon.anchoredPosition.y,
                icon.sizeDelta.x * GlowScale, icon.sizeDelta.y * GlowScale);
        }

        internal static void UpdateFoodColour(FoodSlotUI slot, Sprite icon, ItemDrop.ItemData item, float blink)
        {
            var mode = HudUpdatePlugin.FoodColor != null ? HudUpdatePlugin.FoodColor.Value : FoodIconColorMode.Off;
            var stat = DominantStat(item);
            bool tint = mode == FoodIconColorMode.Tint || mode == FoodIconColorMode.TintAndGlow;
            bool glow = mode == FoodIconColorMode.Glow || mode == FoodIconColorMode.TintAndGlow;

            if (slot.Icon != null)
            {
                var c = tint && IsStyledIcon(icon) ? TintColor(stat) : Color.white;
                c.a = blink;
                slot.Icon.color = c;
            }
            if (slot.Glow == null) return;
            float strength = HudUpdatePlugin.FoodGlowStrength != null ? HudUpdatePlugin.FoodGlowStrength.Value : .7f;
            slot.Glow.enabled = glow && icon != null && strength > 0f;
            if (!slot.Glow.enabled) return;
            slot.Glow.sprite = icon != null && GlowSprites.TryGetValue(icon, out var shaped) ? shaped : RadialGlow();
            var g = GlowColor(stat);
            g.a *= strength * blink;
            slot.Glow.color = g;
        }

        internal static void HideFoodGlow(FoodSlotUI slot)
        {
            if (slot?.Glow != null) slot.Glow.enabled = false;
        }

        // Soft halo in the shape of the icon: alpha downsampled into a padded
        // 64x64 canvas and box-blurred three times.
        private static Sprite BuildGlowSprite(Texture2D source, string key)
        {
            const int size = 64;
            int inner = Mathf.RoundToInt(size / GlowScale);
            int offset = (size - inner) / 2;
            var a = new float[size * size];
            for (int y = 0; y < inner; y++)
            for (int x = 0; x < inner; x++)
                a[(y + offset) * size + x + offset] =
                    source.GetPixelBilinear((x + .5f) / inner, (y + .5f) / inner).a;
            var tmp = new float[size * size];
            for (int pass = 0; pass < 3; pass++)
            {
                BoxBlur(a, tmp, size, 3, true);
                BoxBlur(tmp, a, size, 3, false);
            }
            float max = 0f;
            foreach (float v in a) max = Mathf.Max(max, v);
            if (max <= 0f) max = 1f;
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Pow(a[i] / max, .8f) * 255f));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = key + "_glow" };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            sprite.name = key + "_glow";
            UnityEngine.Object.DontDestroyOnLoad(texture);
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        private static void BoxBlur(float[] src, float[] dst, int size, int radius, bool horizontal)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int xx = horizontal ? x + k : x, yy = horizontal ? y : y + k;
                    if (xx < 0 || yy < 0 || xx >= size || yy >= size) continue;
                    sum += src[yy * size + xx];
                }
                dst[y * size + x] = sum / (radius * 2 + 1);
            }
        }

        // Fallback for vanilla icons (their textures are not CPU-readable).
        private static Sprite RadialGlow()
        {
            if (radialGlow != null) return radialGlow;
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f, dy = (y + .5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float v = Mathf.Clamp01(1f - d);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(v * v * 255f));
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            radialGlow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            UnityEngine.Object.DontDestroyOnLoad(texture);
            UnityEngine.Object.DontDestroyOnLoad(radialGlow);
            return radialGlow;
        }

        // Prefab name -> embedded "food_<glyph>" icon drawn in the reference style.
        private static readonly Dictionary<string, string> FoodGlyphByPrefab =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Raspberry", "raspberry" },
            { "Blueberries", "berries" }, { "Cloudberry", "berries" }, { "Vineberry", "berries" },
            { "Mushroom", "mushroom" }, { "MushroomYellow", "mushroom" }, { "MushroomBlue", "mushroom" },
            { "MushroomJotunPuffs", "mushroom" }, { "MushroomMagecap", "mushroom" }, { "MushroomSmokePuff", "mushroom" },
            { "MagicallyStuffedShroom", "mushroom" },
            { "Carrot", "carrot" }, { "Turnip", "turnip" }, { "Onion", "onion" },
            { "Honey", "honey" }, { "RoyalJelly", "honey" },
            { "CookedMeat", "steak" }, { "CookedDeerMeat", "steak" }, { "CookedWolfMeat", "steak" },
            { "CookedLoxMeat", "steak" }, { "SerpentMeatCooked", "steak" }, { "CookedBugMeat", "steak" },
            { "CookedAsksvinMeat", "steak" }, { "CookedVoltureMeat", "steak" }, { "CookedBoneMawSerpentMeat", "steak" },
            { "MeatPlatter", "steak" },
            { "NeckTailGrilled", "drumstick" }, { "CookedChickenMeat", "drumstick" }, { "CookedHareMeat", "drumstick" },
            { "HoneyGlazedChicken", "drumstick" },
            { "FishCooked", "fish" }, { "FishWraps", "fish" }, { "FishAndBread", "fish" },
            { "CookedEgg", "egg" }, { "MushroomOmelette", "omelette" },
            { "BoarJerky", "jerky" }, { "WolfJerky", "jerky" }, { "WolfMeatSkewer", "skewer" },
            { "CarrotSoup", "stew" }, { "DeerStew", "stew" }, { "TurnipStew", "stew" }, { "SerpentStew", "stew" },
            { "BlackSoup", "stew" }, { "MinceMeatSauce", "stew" }, { "YggdrasilPorridge", "stew" },
            { "SizzlingBerryBroth", "stew" }, { "FierySvinstew", "stew" }, { "MashedMeat", "stew" },
            { "ScorchingMedley", "stew" },
            { "QueensJam", "jam" }, { "SpicyMarmalade", "jam" },
            { "ShocklateSmoothie", "drink" }, { "SparklingShroomshake", "drink" },
            { "Bread", "bread" },
            { "LoxPie", "pie" }, { "PiquantPie", "pie" }, { "RoastedCrustPie", "pie" }, { "MisthareSupreme", "pie" },
            { "Salad", "salad" }, { "MarinatedGreens", "salad" },
            { "Eyescream", "icecream" }, { "Fiddleheadfern", "fern" },
            { "SeekerAspic", "aspic" }, { "BloodPudding", "aspic" },
        };

        // Fallback for foods missing from the table (mods, renamed prefabs):
        // dish words are checked before ingredient words, so "carrotsoup" is a stew.
        private static readonly string[][] FoodGlyphKeywords =
        {
            new[] { "soup", "stew" }, new[] { "broth", "stew" }, new[] { "porridge", "stew" }, new[] { "sauce", "stew" },
            new[] { "medley", "stew" }, new[] { "pie", "pie" }, new[] { "supreme", "pie" }, new[] { "jam", "jam" },
            new[] { "marmalade", "jam" }, new[] { "smoothie", "drink" }, new[] { "shake", "drink" }, new[] { "mead", "drink" },
            new[] { "jerky", "jerky" }, new[] { "skewer", "skewer" }, new[] { "bread", "bread" }, new[] { "salad", "salad" },
            new[] { "greens", "salad" }, new[] { "omelette", "omelette" }, new[] { "egg", "egg" }, new[] { "aspic", "aspic" },
            new[] { "pudding", "aspic" }, new[] { "fish", "fish" }, new[] { "chicken", "drumstick" }, new[] { "hare", "drumstick" },
            new[] { "necktail", "drumstick" }, new[] { "meat", "steak" }, new[] { "raspberr", "raspberry" },
            new[] { "berr", "berries" }, new[] { "shroom", "mushroom" }, new[] { "carrot", "carrot" }, new[] { "turnip", "turnip" },
            new[] { "onion", "onion" }, new[] { "honey", "honey" }, new[] { "jelly", "honey" }, new[] { "fern", "fern" },
            new[] { "cream", "icecream" },
        };

        private static Sprite FoodGlyph(string glyph)
        {
            return glyph != null && FoodIconOverrides.TryGetValue("food_" + glyph, out var sprite) ? sprite : null;
        }

        internal static Sprite ResolveFoodIcon(Player.Food food)
        {
            var item = food?.m_item;
            if (item == null)
                return DefaultFoodIcon;

            string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            string shared = item.m_shared != null ? item.m_shared.m_name : null;

            // Hand-made icons (bundle / embedded, e.g. OnionSoup, Sausages) win.
            if (shared != null && FoodIconOverrides.TryGetValue(shared, out var bySharedName))
                return bySharedName;
            if (prefab != null && FoodIconOverrides.TryGetValue(prefab, out var byPrefabName))
                return byPrefabName;
            string normalized = shared != null ? shared.Replace("$item_", "").Replace("_", "") : null;
            if (normalized != null && FoodIconOverrides.TryGetValue(normalized, out var byNormalized))
                return byNormalized;

            if (prefab != null && FoodGlyphByPrefab.TryGetValue(prefab, out var glyphName))
            {
                var glyph = FoodGlyph(glyphName);
                if (glyph != null) return glyph;
            }

            string haystack = ((prefab ?? "") + " " + (normalized ?? "")).ToLowerInvariant();
            foreach (var rule in FoodGlyphKeywords)
            {
                if (haystack.Contains(rule[0]))
                {
                    var glyph = FoodGlyph(rule[1]);
                    if (glyph != null) return glyph;
                    break;
                }
            }

            if (item.m_shared != null && item.m_shared.m_icons != null && item.m_shared.m_icons.Length > 0)
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

            HudState.RefreshGuardian(__instance);

            float hp = player.GetHealth();
            float maxHp = Mathf.Max(1f, player.GetMaxHealth());
            float target = Mathf.Clamp01(hp / maxHp);

            if (HudState.HpFastValue < 0f)
            {
                HudState.HpFastValue = target;
                HudState.HpSlowValue = target;
            }

            float dt = Time.deltaTime;

            HudState.HpFastValue = target;
            HudState.HpSlowValue = target >= HudState.HpSlowValue
                ? target : Mathf.MoveTowards(HudState.HpSlowValue, target, .3f * dt);

            if (HudState.HpFastImage != null)
                HudState.HpFastImage.fillAmount = HudState.HpFastValue;

            if (HudState.HpSlowImage != null)
                HudState.HpSlowImage.fillAmount = HudState.HpSlowValue;

            HudState.SetChevronFill(HudState.ChevronFast, HudState.HpFastValue);
            HudState.SetChevronFill(HudState.ChevronSlow, HudState.HpSlowValue);

            if (HudState.HpText != null)
                HudState.HpText.text = HudState.ActiveStyle == HudStyle.Chevron
                    ? Mathf.RoundToInt(hp) + " HP"
                    : Mathf.RoundToInt(hp).ToString();

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

            HudState.RefreshGuardian(__instance);
            if (HudState.ActiveStyle == HudStyle.Chevron) return true;

            float stamina = player.GetStamina();
            float maxStamina = Mathf.Max(1f, player.GetMaxStamina());
            float target = Mathf.Clamp01(stamina / maxStamina);

            if (HudState.StaminaFastValue < 0f)
            {
                HudState.StaminaFastValue = target;
                HudState.StaminaSlowValue = target;
            }

            float dt = Time.deltaTime;

            HudState.StaminaFastValue = target;
            HudState.StaminaSlowValue = target >= HudState.StaminaSlowValue
                ? target : Mathf.MoveTowards(HudState.StaminaSlowValue, target, 1.2f * dt);

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
                    slot.Root.SetActive(true);

                    Player.Food food = foods[i];
                    Sprite icon = HudState.ResolveFoodIcon(food);

                    if (slot.Icon != null)
                    {
                        slot.Icon.sprite = icon;
                        slot.Icon.enabled = true;
                        slot.Icon.rectTransform.localScale = Vector3.one;
                    }
                    // Same pulse as vanilla Hud.UpdateFood: once the food can be eaten
                    // again (less than half of its burn time left) the icon blinks.
                    float blink = food.CanEatAgain() ? .7f + Mathf.Sin(Time.time * 5f) * .3f : 1f;
                    HudState.UpdateFoodColour(slot, icon, food.m_item, blink);

                    if (slot.Timer != null)
                    {
                        slot.Timer.text = HudState.FormatTime(food.m_time);
                        slot.Timer.enabled = true;
                    }
                    HudState.SyncTimerShadow(slot);
                }
                else
                {
                    slot.Root.SetActive(true);
                    if (slot.Icon != null)
                    {
                        slot.Icon.sprite = HudState.DefaultFoodIcon;
                        slot.Icon.enabled = HudState.DefaultFoodIcon != null;
                        slot.Icon.color = new Color(1f, 1f, 1f, .35f);
                        slot.Icon.rectTransform.localScale = Vector3.one;
                    }
                    HudState.HideFoodGlow(slot);
                    if (slot.Timer != null) slot.Timer.enabled = false;
                    HudState.SyncTimerShadow(slot);
                }
            }

            return false;
        }
    }
}
