using System.Linq;
using TMPro;
using UnityEngine;

namespace UIReforge
{
    internal static class TmpFontFix
    {
        private static TMP_FontAsset _defaultFont;
        private static bool _logged;

        private static TMP_FontAsset GetDefaultFont()
        {
            if (_defaultFont != null) return _defaultFont;

            // 1) Ищем самый ожидаемый
            _defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(f => f != null && f.name == "LiberationSans SDF");

            // 2) Любой LiberationSans
            if (_defaultFont == null)
            {
                _defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                    .FirstOrDefault(f => f != null && f.name.Contains("LiberationSans"));
            }

            // Prefer the game's loaded sans-serif fonts before a deterministic fallback.
            // Resource enumeration order changes between launches.
            if (_defaultFont == null)
            {
                _defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                    .Where(f => f != null && f.name.IndexOf("Sans", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(f => f.name == "NotoSansJP-Regular SDF" ? 0 : 1)
                    .ThenBy(f => f.name, System.StringComparer.Ordinal)
                    .FirstOrDefault();
            }
            if (_defaultFont == null)
            {
                _defaultFont = TMP_Settings.defaultFontAsset ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                    .Where(f => f != null)
                    .OrderBy(f => f.name, System.StringComparer.Ordinal)
                    .FirstOrDefault();
            }

            if (!_logged)
            {
                _logged = true;
                Debug.Log(_defaultFont != null
                    ? $"[UIReforge] TMP default font: {_defaultFont.name}"
                    : "[UIReforge] TMP default font NOT FOUND");
            }

            return _defaultFont;
        }

        /// <summary>
        /// Самое важное: применить шрифт к ПРЕФАБУ до Instantiate,
        /// чтобы TMP не успел выдать варнинги в Awake/OnEnable.
        /// </summary>
        public static void ApplyFontsToPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null) return;

            var font = GetDefaultFont();
            if (font == null) return;

            // Все TMP тексты (UGUI и обычные) — TMP_Text базовый
            var texts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                if (t.font == null)
                {
                    t.font = font;
                    t.enableAutoSizing = false;
                }
            }

            // Сабмеши тоже иногда ругаются отдельно
            var subMeshes = prefabRoot.GetComponentsInChildren<TMP_SubMeshUI>(true);
            foreach (var sm in subMeshes)
            {
                if (sm == null) continue;

                // если сабмеш без материала/шрифта — пусть подтянется от textComponent
                if (sm.textComponent != null && sm.textComponent.font == null)
                {
                    sm.textComponent.font = font;
                    sm.textComponent.enableAutoSizing = false;
                }
            }
        }

        /// <summary>
        /// На всякий случай — уже для инстанса (не убирает ранние варнинги,
        /// но полезно для динамически созданных TMP в рантайме).
        /// </summary>
        public static void FixFontsOnInstance(GameObject root)
        {
            if (root == null) return;

            var font = GetDefaultFont();
            if (font == null) return;

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                if (t.font == null)
                {
                    t.font = font;
                    t.enableAutoSizing = false;
                    t.ForceMeshUpdate();
                }
            }
        }
    }
}
