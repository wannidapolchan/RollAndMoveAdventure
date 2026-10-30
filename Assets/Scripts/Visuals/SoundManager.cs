using UnityEngine;

/// <summary>
/// Plays sound effects. Uses a few AudioSources in turn, so several sounds
/// can play at once, each with its own pitch. There is no background music
/// on purpose, so every game sound is easy to hear.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private const int SourceCount = 8;
    private AudioSource[] sources;
    private int next;

    private void Awake()
    {
        sources = new AudioSource[SourceCount];
        for (int i = 0; i < SourceCount; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }

        // A scene needs an AudioListener to hear anything (the default camera has one).
        if (FindListener() == null) gameObject.AddComponent<AudioListener>();
    }

    private static AudioListener FindListener()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<AudioListener>();
#else
        return Object.FindObjectOfType<AudioListener>();
#endif
    }

    /// <summary>Plays a sound from Resources/Audio by name.</summary>
    public void Play(string clipName, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = GameAssets.Clip(clipName);
        if (clip == null) return;

        AudioSource source = sources[next];
        next = (next + 1) % SourceCount;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    /// <summary>Plays a sound with a small random pitch change, so repeats sound less robotic.</summary>
    public void PlayVaried(string clipName, float volume = 1f, float pitchRange = 0.08f)
    {
        Play(clipName, volume, 1f + Random.Range(-pitchRange, pitchRange));
    }
}
