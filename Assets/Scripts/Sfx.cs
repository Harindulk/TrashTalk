using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All sounds. Clips are assigned in the Inspector and played by clip name.
public class Sfx : MonoBehaviour
{
    public static Sfx I;

    public AudioClip[] clips;
    public AudioSource[] pool;              // one-shot voices
    public AudioSource thrustSrc, musicSrc; // loops (clips set in the Inspector)
    public float musicVolume = 0.35f;
    public float thrustVolume = 0.4f;
    public float talkVolume = 0.45f;

    readonly Dictionary<string, AudioClip> byName = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    int next;
    bool thrustOn;

    public static bool Muted
    {
        get => PlayerPrefs.GetInt("muted", 0) == 1;
        set
        {
            PlayerPrefs.SetInt("muted", value ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = value ? 0f : 1f;
        }
    }

    public static void Play(string name, float volume = 1f, float pitch = 1f)
    {
        if (I == null || !I.byName.TryGetValue(name, out var clip)) return;
        // Same sound twice in one frame just gets louder and harsher, so skip it.
        if (I.lastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < 0.04f) return;
        I.lastPlayed[name] = Time.unscaledTime;
        I.PlayClip(clip, volume, pitch * Random.Range(0.94f, 1.06f));
    }

    public static void Thrust(bool on)
    {
        if (I != null) I.thrustOn = on;
    }

    public static void Talk(float pitch, int syllables)
    {
        if (I == null || syllables <= 0 || !I.byName.TryGetValue("talk", out var clip)) return;
        I.StartCoroutine(I.TalkRoutine(clip, Mathf.Clamp(pitch, 0.3f, 3f), syllables));
    }

    public static void Music(bool on)
    {
        if (I == null) return;
        if (on && !I.musicSrc.isPlaying) I.musicSrc.Play();
        if (!on) I.musicSrc.Stop();
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(this); return; }
        I = this;
        foreach (var c in clips) if (c != null) byName[c.name] = c;
        thrustSrc.volume = 0f;
        musicSrc.volume = musicVolume;
        AudioListener.volume = Muted ? 0f : 1f;
    }

    void Update()
    {
        // Engine fades in/out instead of clicking; goes quiet while the game is paused.
        float goal = thrustOn && Time.timeScale > 0f ? thrustVolume : 0f;
        thrustSrc.volume = Mathf.MoveTowards(thrustSrc.volume, goal, Time.unscaledDeltaTime * 3f);
        thrustSrc.pitch = 1f + Mathf.Sin(Time.unscaledTime * 13f) * 0.04f;
        if (goal > 0f && !thrustSrc.isPlaying) thrustSrc.Play();
        if (goal == 0f && thrustSrc.volume <= 0f && thrustSrc.isPlaying) thrustSrc.Stop();
    }


    void PlayClip(AudioClip clip, float volume, float pitch)
    {
        AudioSource src = null;
        foreach (var s in pool)
            if (!s.isPlaying) { src = s; break; }
        if (src == null) { src = pool[next]; next = (next + 1) % pool.Length; }
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.Play();
    }

    IEnumerator TalkRoutine(AudioClip clip, float pitch, int syllables)
    {
        for (int i = 0; i < syllables; i++)
        {
            PlayClip(clip, talkVolume, pitch * Random.Range(0.85f, 1.25f));
            yield return new WaitForSecondsRealtime(Random.Range(0.06f, 0.09f));
        }
    }
}
