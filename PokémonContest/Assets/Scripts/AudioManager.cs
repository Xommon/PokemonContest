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
        try
        {
            if (!s.source.isPlaying)
            {
                s.source.Play();
            }
            s.source.volume = gameManager.musicVolume;
        }
        catch
        {
            Debug.LogWarning("Cannot find sound file: " + name);
        }
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