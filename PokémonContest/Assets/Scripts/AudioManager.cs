using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Reflection;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public GameManager gameManager;

    private void Awake()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            if (s.music)
            {
                s.source.volume = gameManager.musicVolume;
            }
            else
            {
                s.source.volume = s.volume;
            }
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            
        }
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Cannot find sound file: " + name);
            return;
        }

        // Music → use normal looping AudioSource
        if (s.music)
        {
            s.source.volume = gameManager.musicVolume;

            if (!s.source.isPlaying)
                s.source.Play();

            return;
        }

        // SFX → allow rapid / overlapping playback
        s.source.pitch = s.pitch;
        s.source.volume = s.volume;
        s.source.PlayOneShot(s.clip, s.volume);
    }

    public void Play(string name, float pitch)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Cannot find sound file: " + name);
            return;
        }

        // Music → overriding pitch isn’t normally desired, but allowed
        if (s.music)
        {
            s.source.pitch = pitch;
            s.source.volume = gameManager.musicVolume;

            if (!s.source.isPlaying)
                s.source.Play();

            return;
        }

        // SFX → use one-shot playback with custom pitch
        s.source.pitch = pitch;
        s.source.volume = s.volume;
        s.source.PlayOneShot(s.clip, s.volume);
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        try
        {
            s.source.Stop();
        }
        catch
        {
            Debug.LogWarning("Cannot find sound file: " + name);
        }
    }
}