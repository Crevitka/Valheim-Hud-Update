using BepInEx;
using System;
using System.IO;
using System.Text;
using UnityEngine;

// One-off helper: saves the vanilla icons of every food and guardian power to
// BepInEx/HUD-Update-IconDump/ once a world is loaded, then does nothing.
[BepInPlugin("crevitka.hudupdate.icondump", "HUD Update Icon Dump", "1.0.1")]
public class IconDumpPlugin : BaseUnityPlugin
{
    private bool done;

    private void Update()
    {
        if (done || Player.m_localPlayer == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0) return;
        done = true;
        string dir = Path.Combine(Paths.BepInExRootPath, "HUD-Update-IconDump");
        Directory.CreateDirectory(Path.Combine(dir, "food"));
        Directory.CreateDirectory(Path.Combine(dir, "guardian"));
        var list = new StringBuilder("prefab\tname\thealth\tstamina\teitr\ttype\n");
        int n = 0;
        foreach (var go in ObjectDB.instance.m_items)
        {
            try
            {
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                var s = drop != null ? drop.m_itemData.m_shared : null;
                if (s == null || s.m_icons == null || s.m_icons.Length == 0) continue;
                if (s.m_food <= 0f && s.m_foodStamina <= 0f && s.m_foodEitr <= 0f) continue;
                Save(s.m_icons[0], Path.Combine(Path.Combine(dir, "food"), go.name + ".png"));
                list.Append(go.name).Append('\t').Append(s.m_name).Append('\t').Append(s.m_food).Append('\t')
                    .Append(s.m_foodStamina).Append('\t').Append(s.m_foodEitr).Append('\t').Append(s.m_itemType).Append('\n');
                n++;
            }
            catch (Exception ex) { Logger.LogWarning(go.name + ": " + ex.Message); }
        }
        foreach (var se in ObjectDB.instance.m_StatusEffects)
        {
            try
            {
                if (se != null && se.name.StartsWith("GP_") && se.m_icon != null)
                    Save(se.m_icon, Path.Combine(Path.Combine(dir, "guardian"), se.name + ".png"));
            }
            catch (Exception ex) { Logger.LogWarning(se.name + ": " + ex.Message); }
        }
        File.WriteAllText(Path.Combine(dir, "foods.tsv"), list.ToString());
        Logger.LogInfo("Saved " + n + " food icons to " + dir);
    }

    private static void Save(Sprite sprite, string path)
    {
        Rect r = sprite.textureRect;
        var src = sprite.texture;
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;
        var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(r.x, r.y, r.width, r.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
        UnityEngine.Object.Destroy(tex);
    }
}
