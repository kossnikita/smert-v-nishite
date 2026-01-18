using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace smert_v_nishite;

public class TextureReplacer : MonoBehaviour
{
    private const string ATLAS_NAME = "arctic small valuables_defaultmaterial_emissive";
    private const int OVERLAY_X = 241;
    private const int OVERLAY_Y_FROM_TOP = 107;

    private static readonly HashSet<int> processedMaterials = new();
    private static readonly Dictionary<int, Texture2D> modifiedAtlasCache = new();
    private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

    private float lastScanTime;

    private void Start() => StartCoroutine(InitialScan());

    private IEnumerator InitialScan()
    {
        yield return new WaitForSeconds(0.5f);
        ScanRenderers();
    }

    private void Update()
    {
        if (Time.time - lastScanTime < 2f) return;
        lastScanTime = Time.time;
        ScanRenderers();
    }

    private void ScanRenderers()
    {
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            if (renderer == null) continue;
            TryReplacePropertyBlock(renderer);
            TryReplaceMaterials(renderer);
        }
    }

    private void TryReplacePropertyBlock(Renderer renderer)
    {
        var mpb = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(mpb);

        if (mpb.GetTexture(EmissionMapId) is not Texture2D tex) return;
        if (tex.name?.ToLowerInvariant() != ATLAS_NAME) return;

        var modified = GetOrCreateModifiedAtlas(tex);
        if (modified == null) return;

        mpb.SetTexture(EmissionMapId, modified);
        renderer.SetPropertyBlock(mpb);
    }

    private void TryReplaceMaterials(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials == null) return;

        foreach (var mat in materials)
        {
            if (mat == null) continue;
            int id = mat.GetInstanceID();
            if (processedMaterials.Contains(id)) continue;
            processedMaterials.Add(id);

            if (TryReplaceMaterialTexture(mat, "_EmissionMap")) continue;
            if (TryReplaceMaterialTexture(mat, "_MainTex")) continue;
            TryReplaceMaterialTexture(mat, "_BaseMap");
        }
    }

    private bool TryReplaceMaterialTexture(Material mat, string prop)
    {
        if (!mat.HasProperty(prop)) return false;
        if (mat.GetTexture(prop) is not Texture2D tex) return false;
        if (tex.name?.ToLowerInvariant() != ATLAS_NAME) return false;

        var modified = GetOrCreateModifiedAtlas(tex);
        if (modified == null) return false;

        mat.SetTexture(prop, modified);
        if (prop == "_EmissionMap")
            try { mat.EnableKeyword("_EMISSION"); } catch { }

        return true;
    }

    private Texture2D GetOrCreateModifiedAtlas(Texture2D original)
    {
        int key = original.GetInstanceID();
        if (modifiedAtlasCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var modified = CreateModifiedAtlas(original);
        if (modified != null)
            modifiedAtlasCache[key] = modified;
        return modified;
    }

    private Texture2D CreateModifiedAtlas(Texture2D original)
    {
        if (original == null || Plugin.replacementTexture == null) return null;

        var overlay = Plugin.replacementTexture;
        int yBottom = original.height - OVERLAY_Y_FROM_TOP - overlay.height;

        // Copy original via RenderTexture (handles non-readable textures)
        var rt = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(original, rt);

        var result = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false, true);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, original.width, original.height), 0, 0);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // Read overlay pixels
        var overlayPixels = ReadPixels(overlay);
        if (overlayPixels == null) return null;

        result.SetPixels(OVERLAY_X, yBottom, overlay.width, overlay.height, overlayPixels);
        result.Apply(false);
        result.wrapMode = TextureWrapMode.Clamp;
        result.filterMode = FilterMode.Trilinear;

        return result;
    }

    private Color[] ReadPixels(Texture2D tex)
    {
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(tex, rt);

        var temp = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        temp.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        temp.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var pixels = temp.GetPixels();
        Destroy(temp);
        return pixels;
    }

    private void OnDestroy()
    {
        processedMaterials.Clear();
        foreach (var tex in modifiedAtlasCache.Values)
            if (tex != null) Destroy(tex);
        modifiedAtlasCache.Clear();
    }
}
