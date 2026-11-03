using System.Collections;
using UnityEngine;

// Plays the clip of the AudioSource once, fading it in at the start and out at the end.
// Started by the incense burner when the first piece of wood is put in.
[RequireComponent(typeof(AudioSource))]
public class SoundtrackPlayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float volume = 0.5f;
    [SerializeField] float fadeInSeconds = 4f;
    [SerializeField] float fadeOutSeconds = 8f;
    [SerializeField] float startDelaySeconds;     // wait this long after Play() before the clip starts

    AudioSource source;
    bool started;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;      // 2D: heard the same everywhere in the temple
    }

    public void Play()
    {
        if (started || source.clip == null) return;
        started = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (startDelaySeconds > 0f) yield return new WaitForSeconds(startDelaySeconds);
        source.volume = fadeInSeconds > 0f ? 0f : volume;
        source.Play();
        float length = source.clip.length;

        while (source.isPlaying)
        {
            float t = source.time;
            float fadeIn = fadeInSeconds > 0f ? Mathf.Clamp01(t / fadeInSeconds) : 1f;
            float fadeOut = fadeOutSeconds > 0f ? Mathf.Clamp01((length - t) / fadeOutSeconds) : 1f;
            source.volume = volume * Mathf.Min(fadeIn, fadeOut);
            yield return null;
        }
    }
}
