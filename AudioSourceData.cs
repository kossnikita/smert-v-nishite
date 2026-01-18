using UnityEngine;

namespace smert_v_nishite;

public class AudioSourceData
{
    public AudioClip OriginalClip { get; set; }
    public AudioClip RealClip
    {
        get { using (new SpoofBypass()) return Source.clip; }
        set { using (new SpoofBypass()) Source.clip = value; }
    }
    public AudioSource Source { get; }

    private AudioSourceData(AudioSource source) => Source = source;

    public static AudioSourceData GetOrCreate(AudioSource source)
    {
        if (Plugin.audioSourceData.TryGetValue(source, out var data))
            return data;

        data = new AudioSourceData(source) { OriginalClip = source.clip };
        Plugin.audioSourceData[source] = data;
        return data;
    }
}
