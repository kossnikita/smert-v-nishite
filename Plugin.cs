using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace smert_v_nishite;

[BepInPlugin("smert-v-nishite", "Smert v Nishite", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new BepInEx.Logging.ManualLogSource Logger;
    internal static AudioClip replacementAudio;
    internal static Texture2D replacementTexture;
    internal static Dictionary<AudioSource, AudioSourceData> audioSourceData = new();

    private static bool textureReplacerInitialized;

    private void Awake()
    {
        Logger = base.Logger;

        if (!LoadAssetBundle()) return;

        new Harmony("smert-v-nishite").PatchAll();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        Logger.LogInfo("Plugin loaded!");
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (textureReplacerInitialized) return;
        textureReplacerInitialized = true;

        var go = new GameObject("TextureReplacer");
        DontDestroyOnLoad(go);
        go.AddComponent<TextureReplacer>();
    }

    private bool LoadAssetBundle()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("smert_v_nishite.smert_v_nishite");
        if (stream == null)
        {
            Logger.LogError("Embedded asset bundle not found.");
            return false;
        }

        byte[] data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);

        var bundle = AssetBundle.LoadFromMemory(data);
        if (bundle == null)
        {
            Logger.LogError("Failed to load asset bundle.");
            return false;
        }

        foreach (string name in bundle.GetAllAssetNames())
        {
            if (name.EndsWith(".ogg"))
                replacementAudio = bundle.LoadAsset<AudioClip>(name);
            else if (name.EndsWith(".png"))
                replacementTexture = bundle.LoadAsset<Texture2D>(name);
        }

        return true;
    }

    public static AudioClip GetReplacedSound(string originalName)
    {
        return originalName.ToLowerInvariant() == "phone ringtone" ? replacementAudio : null;
    }
}
