using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
            UniTask.Void(Test);
        }

        private async UniTaskVoid Test()
        {
            await UniTask.Delay(5000);
            Debug.Log("AudioManager Test");
            LoadAudioClip("Zephyrs", "BackGroundMusic");
        }

        private void RegisterAudioClips()
        {
            RegisteredAudioClips = new();
            foreach (AudioClip AudioClip in AudioClips)
            {
                RegisteredAudioClips.Add(AudioClip.name, AudioClip);
            }
        }

        private void RegisterAudioSources()
        {
            RegisteredAudioSources = new();
            foreach (AudioSource AudioSource in AudioSources)
            {
                RegisteredAudioSources.Add(AudioSource.name, AudioSource);
            }
        }

        public void CloseAudioSource(AudioSource Source)
        {
            if (Source is not null)
            {
                Source.enabled = false;
            }
        }

        public void CloseAudioSource(string closedSource)
        {
            if (closedSource is not null)
            {
                if (RegisteredAudioSources.TryGetValue(closedSource, out AudioSource closedRef))
                {
                    closedRef.enabled = false;
                }
                else
                {
                    Debug.LogWarning($"Audio source '{closedSource}' not found.");
                }
            }
        }

        public void CloseAudioSource(string[] closedSources)
        {
            if (closedSources is not null)
            {
                foreach (string Source in closedSources)
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

        public AudioSource RequireAudioSource(string requiredSource)
        {
            if (requiredSource is not null)
            {
                if (RegisteredAudioSources.TryGetValue(requiredSource, out AudioSource requiredRef))
                {
                    requiredRef.enabled = true;

                    return requiredRef;
                }
                else
                {
                    Debug.LogWarning($"Audio source '{requiredSource}' not found.");

                    throw new ArgumentNullException();
                }
            }

            throw new ArgumentNullException("RequiredSource String Null");
        }

        public List<AudioSource> RequireAudioSource(string[] requiredSources)
        {
            List<AudioSource> returnSources = new();

            if (requiredSources is not null)
            {
                foreach (string source in requiredSources)
                {
                    if (source is null)
                    {
                        returnSources.Add(RequireAudioSource(source));
                    }
                }

                return returnSources;
            }

            throw new ArgumentNullException();
        }

        public AudioClip RequireAudioClip(string requiredClip)
        {
            if (requiredClip is not null)
            {
                if (RegisteredAudioClips.TryGetValue(requiredClip, out AudioClip returnedRef))
                {
                    return returnedRef;
                }
                else
                {
                    Debug.LogWarning($"Audio clip '{requiredClip}' not found.");
                    throw new ArgumentNullException();
                }
            }

            throw new ArgumentNullException("RequiredClip String Null");
        }

        /// <summary>
        /// 安全的加载音频片段
        /// </summary>
        /// <param name="clipName"></param>
        /// <param name="sourceName"></param>
        public void LoadAudioClip(string clipName, string sourceName)
        {
            AudioSource source = RequireAudioSource(sourceName);

            AudioClip clip = RequireAudioClip(clipName);

            source.clip = clip;

            source.Play();
        }
    }
}
