using System;
using System.Collections.Generic;
using Singleton;
using UnityEngine;

namespace AudioNS
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Ext.ReadOnlyInGame, SerializeField]
        List<AudioSource> AudioSources;

        [Ext.ReadOnlyInGame, SerializeField]
        List<AudioClip> AudioClips;

        private Dictionary<string, AudioClip> RegisteredAudioClips;

        private Dictionary<string, AudioSource> RegisteredAudioSources;

        protected override void SingletonAwake()
        {
            RegisterAudioClips();
            RegisterAudioSources();
        }

        private void RegisterAudioClips()
        {
            RegisteredAudioClips = new();
            foreach (AudioClip AudioClip in AudioClips)
            {
                RegisteredAudioClips.Add(AudioClip.name, AudioClip);
                Debug.Log(AudioClip.name);
            }
        }

        private void RegisterAudioSources()
        {
            RegisteredAudioSources = new();
            foreach (AudioSource AudioSource in AudioSources)
            {
                RegisteredAudioSources.Add(AudioSource.name, AudioSource);
                Debug.Log(AudioSource.name);
            }
        }

        public void CloseAudioSource(AudioSource Source)
        {
            if (Source is not null)
            {
                Source.enabled = false;
            }
        }

        public void CloseAudioSource(string ClosedSource)
        {
            if (ClosedSource is not null)
            {
                if (RegisteredAudioSources.TryGetValue(ClosedSource, out AudioSource Closed))
                {
                    Closed.enabled = false;
                }
                else
                {
                    Debug.LogWarning($"Audio source '{ClosedSource}' not found.");
                }
            }
        }

        public void CloseAudioSource(string[] ClosedSources)
        {
            if (ClosedSources is not null)
            {
                foreach (string Source in ClosedSources)
                {
                    CloseAudioSource(Source);
                }
            }
        }

        public void CloseAllAudioSource()
        {
            foreach (AudioSource Source in RegisteredAudioSources.Values)
            {
                CloseAudioSource(Source);
            }
        }

        public AudioSource RequireAudioSource(string RequiredSource)
        {
            if (RequiredSource is not null)
            {
                if (RegisteredAudioSources.TryGetValue(RequiredSource, out AudioSource Required))
                {
                    Required.enabled = true;

                    return Required;
                }
                else
                {
                    Debug.LogWarning($"Audio source '{RequiredSource}' not found.");

                    throw new ArgumentNullException();
                }
            }

            throw new ArgumentNullException("RequiredSource String Null");
        }

        public List<AudioSource> RequireAudioSource(string[] RequiredSources)
        {
            List<AudioSource> ReturnSources = new();

            if (RequiredSources is not null)
            {
                foreach (string Source in RequiredSources)
                {
                    ReturnSources.Add(RequireAudioSource(Source));
                }

                return ReturnSources;
            }

            throw new ArgumentNullException();
        }

        public AudioClip RequireAudioClip(string RequiredClip)
        {
            if (RequiredClip is not null)
            {
                if (RegisteredAudioClips.TryGetValue(RequiredClip, out AudioClip Required))
                {
                    return Required;
                }
                else
                {
                    Debug.LogWarning($"Audio clip '{RequiredClip}' not found.");
                    throw new ArgumentNullException();
                }
            }
            throw new ArgumentNullException("RequiredClip String Null");
        }

        /// <summary>
        /// 安全的加载音频片段
        /// </summary>
        /// <param name="ClipName"></param>
        /// <param name="SourceName"></param>
        public void LoadAudioClip(string ClipName, string SourceName)
        {
            AudioSource Source = RequireAudioSource(SourceName);

            AudioClip Clip = RequireAudioClip(ClipName);

            Source.clip = Clip;

            Source.Play();
        }
    }
}
