using HarmonyLib;
using UnityEngine;
using System;

namespace smert_v_nishite;

[HarmonyPatch(typeof(AudioSource))]
public class AudioSourcePatch
{
    [ThreadStatic]
    internal static bool bypassSpoofing;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(AudioSource.Play), new Type[] { })]
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
    static bool Play(AudioSource __instance)
    {
        var data = AudioSourceData.GetOrCreate(__instance);
        var replacement = Plugin.GetReplacedSound(data.OriginalClip?.name ?? "");
        if (replacement == null) return true;

        data.RealClip = replacement;
        using (new SpoofBypass()) __instance.clip = replacement;
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), new[] { typeof(AudioClip), typeof(float) })]
    static bool PlayOneShot(ref AudioClip clip)
    {
        var replacement = Plugin.GetReplacedSound(clip?.name ?? "");
        if (replacement != null) clip = replacement;
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(AudioSource.clip), MethodType.Setter)]
    static bool ClipSetter(AudioSource __instance, AudioClip value)
    {
        if (bypassSpoofing) return true;

        if (Plugin.audioSourceData.TryGetValue(__instance, out var data) && data.OriginalClip == value)
            return false;

        AudioSourceData.GetOrCreate(__instance).OriginalClip = value;
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(AudioSource.clip), MethodType.Getter)]
    static void ClipGetter(AudioSource __instance, ref AudioClip __result)
    {
        if (bypassSpoofing) return;
        if (Plugin.audioSourceData.TryGetValue(__instance, out var data) && data.OriginalClip != null)
            __result = data.OriginalClip;
    }
}

public class SpoofBypass : IDisposable
{
    public SpoofBypass() => AudioSourcePatch.bypassSpoofing = true;
    public void Dispose() => AudioSourcePatch.bypassSpoofing = false;
}
