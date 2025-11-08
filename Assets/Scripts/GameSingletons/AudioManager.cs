using System;
using System.Collections.Generic;
using System.Threading;
using AudioRegistry;
using Cysharp.Threading.Tasks;
using Singleton;
using UnityEngine;

namespace AudioNS
{
    public readonly struct Source : ISource
    {
        public string Value { get; }

        private Source(string value) => Value = value;

        // Music
        public static Source BGM = new Source("BackGroundMusic");

        // Fx
        public static Source Track0 = new Source("Track0Fx");
        public static Source Track1 = new Source("Track1Fx");
        public static Source Track2 = new Source("Track2Fx");
        public static Source Track3 = new Source("Track3Fx");

        public static Source UI = new Source("UIFx");

        public static implicit operator string(Source source) => source.Value;
    }

    public class AudioManager : Singleton<AudioManager>
    {
        private class AudioSourceState
        {
            public AudioClip CurrentClip { get; set; } = null;
            public float Volume { get; set; } = 0.8f;
            public bool IsWarmedUp { get; set; } = false;
            public AudioClip WarmedUpClip { get; set; } = null;
            public CancellationTokenSource RewarmToken { get; set; } = new();
            public bool IsProcessing { get; set; } = false;
            public string PendingClipName { get; set; } = null;
        }

        [SerializeField]
        List<AudioSource> AudioSources;

        [SerializeField]
        List<AudioClip> AudioClips;

        [SerializeField]
        List<AudioClip> SFXClips;

        private readonly Dictionary<string, AudioClip> _registeredAudioClips = new();
        private readonly Dictionary<string, AudioSource> _registeredAudioSources = new();
        private readonly Dictionary<string, AudioSourceState> _sourceStates = new();

        protected override void SingletonAwake()
        {
            RegisterAudioClips();
            RegisterAudioSources();
            UniTask.Void(PrewarmAllAudioSources);
        }

        private void RegisterAudioClips()
        {
            foreach (AudioClip audioClip in AudioClips)
            {
                _registeredAudioClips.Add(audioClip.name, audioClip);
            }

            foreach (AudioClip audioClip in SFXClips)
            {
                _registeredAudioClips.Add(audioClip.name, audioClip);
            }
        }

        private void RegisterAudioSources()
        {
            foreach (AudioSource audioSource in AudioSources)
            {
                _registeredAudioSources.Add(audioSource.name, audioSource);
                _sourceStates.Add(audioSource.name, new AudioSourceState());
            }
        }

        private async UniTaskVoid PrewarmAllAudioSources()
        {
            foreach (var sourcePair in _registeredAudioSources)
            {
                string sourceName = sourcePair.Key;
                AudioSource source = sourcePair.Value;

                if (SFXClips.Count > 2)
                {
                    AudioClip dummyClip = SFXClips[2];
                    var state = _sourceStates[sourceName];

                    source.clip = dummyClip;
                    source.volume = 0f;
                    source.Play();
                    source.Pause();

                    state.IsWarmedUp = true;
                    state.WarmedUpClip = dummyClip;
                    state.RewarmToken = new CancellationTokenSource();

                    LogManager.Log(
                        $"加载时预热源 {source.name} , 预热片段 {dummyClip.name}",
                        nameof(AudioManager),
                        false
                    );
                }
            }

            await UniTask.WaitForSeconds(0);
        }

        private void CloseAudioSource(AudioSource source)
        {
            if (source != null)
            {
                source.enabled = false;
            }
        }

        public void CloseAudioSource(Source closedSource)
        {
            if (_registeredAudioSources.TryGetValue(closedSource, out AudioSource closedRef))
            {
                closedRef.enabled = false;
            }
            else
            {
                LogManager.Warning($"Audio source '{closedSource}' not found.");
            }
        }

        public void CloseAudioSource(Source[] closedSources)
        {
            if (closedSources != null)
            {
                foreach (Source source in closedSources)
                {
                    CloseAudioSource(source);
                }
            }
        }

        public void CloseAllAudioSource()
        {
            foreach (AudioSource source in _registeredAudioSources.Values)
            {
                CloseAudioSource(source);
            }
        }

        public AudioSource RequireAudioSource(ISource requiredSource)
        {
            return RequireAudioSource(requiredSource.Value);
        }

        private AudioSource RequireAudioSource(string requiredSource)
        {
            if (_registeredAudioSources.TryGetValue(requiredSource, out AudioSource requiredRef))
            {
                requiredRef.enabled = true;
                return requiredRef;
            }
            else
            {
                LogManager.Warning($"Audio source '{requiredSource}' not found.");
                throw new ArgumentNullException();
            }
        }

        public List<AudioSource> RequireAudioSource(ISource[] requiredSources)
        {
            List<AudioSource> returnSources = new();

            if (requiredSources != null)
            {
                foreach (Source source in requiredSources)
                {
                    returnSources.Add(RequireAudioSource(source.Value));
                }

                return returnSources;
            }

            throw new ArgumentNullException();
        }

        private AudioClip RequireAudioClip(string requiredClip)
        {
            if (requiredClip != null)
            {
                if (_registeredAudioClips.TryGetValue(requiredClip, out AudioClip returnedRef))
                {
                    return returnedRef;
                }
                else
                {
                    LogManager.Warning($"Audio clip '{requiredClip}' not found.");
                    throw new ArgumentNullException();
                }
            }

            throw new ArgumentNullException("RequiredClip String Null");
        }

        // 添加这些方法到 AudioManager 类中

        /// <summary>
        /// 使用外部AudioClip加载音频
        /// </summary>
        /// <param name="audioClip">外部AudioClip</param>
        /// <param name="sourceEnum">音频源</param>
        public void LoadAudioClip(AudioClip audioClip, ISource sourceEnum)
        {
            LoadAudioClip(audioClip, sourceEnum.Value);
        }

        /// <summary>
        /// 使用外部AudioClip加载音频
        /// </summary>
        /// <param name="audioClip">外部AudioClip</param>
        /// <param name="sourceEnum">音频源</param>
        public void LoadAudioClip(AudioClip audioClip, Source sourceEnum)
        {
            LoadAudioClip(audioClip, sourceEnum.Value);
        }

        /// <summary>
        /// 使用外部AudioClip加载音频（核心实现）
        /// </summary>
        /// <param name="audioClip">外部AudioClip</param>
        /// <param name="sourceEnum">音频源名称</param>
        /// <param name="output">是否输出日志</param>
        private async void LoadAudioClip(
            AudioClip audioClip,
            string sourceEnum,
            bool output = false
        )
        {
            if (audioClip == null)
            {
                LogManager.Warning($"External audio clip is null for source: {sourceEnum}");
                return;
            }

            if (!_sourceStates.ContainsKey(sourceEnum))
            {
                _sourceStates[sourceEnum] = new AudioSourceState();
            }

            AudioSourceState state = _sourceStates[sourceEnum];
            AudioSource source = RequireAudioSource(sourceEnum);

            if (state.IsProcessing)
            {
                state.PendingClipName = audioClip.name; // 使用clip名称作为pending标识
                LogManager.Log(
                    $"音频源忙，外部音频请求加入队列: {audioClip.name}",
                    nameof(AudioManager),
                    output
                );
                return;
            }

            state.IsProcessing = true;

            try
            {
                await ProcessExternalAudioLoad(audioClip, sourceEnum, source, state, output);

                if (state.PendingClipName != null)
                {
                    // 对于外部音频，我们无法重新创建AudioClip，所以只能记录警告
                    LogManager.Warning($"有挂起的外部音频请求但无法处理: {state.PendingClipName}");
                    state.PendingClipName = null;
                }
            }
            finally
            {
                state.IsProcessing = false;
            }
        }

        /// <summary>
        /// 处理外部AudioClip的加载
        /// </summary>
        private async UniTask ProcessExternalAudioLoad(
            AudioClip audioClip,
            string sourceEnum,
            AudioSource source,
            AudioSourceState state,
            bool output
        )
        {
            CancelRewarmTask(sourceEnum);
            await UniTask.Yield();

            if (source.isPlaying && state.CurrentClip == audioClip)
            {
                source.time = 0f;
                LogManager.Log(
                    $"重置已播放外部音频时间: {audioClip.name}",
                    nameof(AudioManager),
                    output
                );
                return;
            }

            if (source.isPlaying)
            {
                source.Stop();
                await UniTask.DelayFrame(1);
            }

            bool needWarmup = !state.IsWarmedUp || state.WarmedUpClip != audioClip;

            if (needWarmup)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                    await UniTask.DelayFrame(1);
                }

                source.clip = audioClip;
                source.volume = 0f;
                source.Play();
                source.Pause();

                state.IsWarmedUp = true;
                state.WarmedUpClip = audioClip;

                LogManager.Log($"预热外部音频片段: {audioClip.name}", nameof(AudioManager), output);
            }

            if (source.isPlaying)
            {
                source.Stop();
                await UniTask.DelayFrame(1);
            }

            source.time = 0f;
            source.volume = state.Volume;
            source.Play();

            state.CurrentClip = audioClip;

            _ = RewarmAfterPlayAsync(source, sourceEnum, state.Volume);

            LogManager.Log(
                $"播放外部音频 {audioClip.name}, 源: {sourceEnum}",
                nameof(AudioManager),
                output
            );
        }

        /// <summary>
        /// 批量加载外部AudioClip
        /// </summary>
        /// <param name="clipPacks">音频包列表 (AudioClip, Source)</param>
        public void LoadExternalAudioClips(List<(AudioClip, Source)> clipPacks)
        {
            foreach (var clipPack in clipPacks)
            {
                LoadAudioClip(clipPack.Item1, clipPack.Item2);
            }
        }

        /// <summary>
        /// 批量加载外部AudioClip
        /// </summary>
        /// <param name="clipPacks">音频包列表 (AudioClip, ISource)</param>
        public void LoadExternalAudioClips(List<(AudioClip, ISource)> clipPacks)
        {
            foreach (var clipPack in clipPacks)
            {
                LoadAudioClip(clipPack.Item1, clipPack.Item2);
            }
        }

        public void LoadAudioClip<T1, T2>(T1 clipEnum, T2 sourceEnum)
            where T1 : IAudio
            where T2 : ISource
        {
            LoadAudioClip(clipEnum.Value, sourceEnum.Value);
        }

        private async void LoadAudioClip(string clipName, string sourceEnum, bool output = false)
        {
            if (!_sourceStates.ContainsKey(sourceEnum))
            {
                _sourceStates[sourceEnum] = new AudioSourceState();
            }

            AudioSourceState state = _sourceStates[sourceEnum];
            AudioSource source = RequireAudioSource(sourceEnum);
            AudioClip newClip = RequireAudioClip(clipName);

            if (state.IsProcessing)
            {
                state.PendingClipName = clipName;
                LogManager.Log($"音频源忙，请求加入队列: {clipName}", nameof(AudioManager), output);
                return;
            }

            state.IsProcessing = true;

            try
            {
                await ProcessAudioLoad(clipName, sourceEnum, newClip, source, state, output);

                if (state.PendingClipName != null)
                {
                    string pendingClip = state.PendingClipName;
                    state.PendingClipName = null;
                    await UniTask.DelayFrame(1);
                    LoadAudioClip(pendingClip, sourceEnum, output);
                }
            }
            finally
            {
                state.IsProcessing = false;
            }
        }

        private async UniTask ProcessAudioLoad(
            string clipName,
            string sourceEnum,
            AudioClip newClip,
            AudioSource source,
            AudioSourceState state,
            bool output
        )
        {
            CancelRewarmTask(sourceEnum);
            await UniTask.Yield();

            if (source.isPlaying && state.CurrentClip == newClip)
            {
                source.time = 0f;
                LogManager.Log($"重置已播放音频时间: {clipName}", nameof(AudioManager), output);
                return;
            }

            if (source.isPlaying)
            {
                source.Stop();
                await UniTask.DelayFrame(1);
            }

            bool needWarmup = !state.IsWarmedUp || state.WarmedUpClip != newClip;

            if (needWarmup)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                    await UniTask.DelayFrame(1);
                }

                source.clip = newClip;
                source.volume = 0f;
                source.Play();
                source.Pause();

                state.IsWarmedUp = true;
                state.WarmedUpClip = newClip;

                LogManager.Log($"预热新片段: {newClip.name}", nameof(AudioManager), output);
            }

            if (source.isPlaying)
            {
                source.Stop();
                await UniTask.DelayFrame(1);
            }

            source.time = 0f;
            source.volume = state.Volume;
            source.Play();

            state.CurrentClip = newClip;

            _ = RewarmAfterPlayAsync(source, sourceEnum, state.Volume);

            LogManager.Log($"播放音频 {clipName}, 源: {sourceEnum}", nameof(AudioManager), output);
        }

        private async UniTaskVoid RewarmAfterPlayAsync(
            AudioSource source,
            string sourceEnum,
            float targetVolume
        )
        {
            if (!_sourceStates.TryGetValue(sourceEnum, out var state))
                return;

            var cancellationToken = state.RewarmToken.Token;

            try
            {
                if (!source.isPlaying || source.clip == null)
                {
                    LogManager.Warning(
                        $"音频源未播放，跳过重预热: {sourceEnum}",
                        nameof(AudioManager)
                    );
                    return;
                }

                float clipLength = source.clip.length - source.time;

                float elapsed = 0f;
                while (elapsed < clipLength && !cancellationToken.IsCancellationRequested)
                {
                    if (!source.isPlaying || state.CurrentClip != source.clip)
                        return;

                    await UniTask.DelayFrame(1, cancellationToken: cancellationToken);
                    elapsed += Time.deltaTime;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (state.CurrentClip == source.clip && !cancellationToken.IsCancellationRequested)
                {
                    source.volume = 0f;
                    source.time = 0f;
                    source.Play();
                    source.Pause();

                    LogManager.Log($"重预热音频 {source.clip.name}", nameof(AudioManager), false);
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消是正常情况
            }
        }

        public void LoadAudioClips<T1, T2>(List<(T1, T2)> clipPacks)
            where T1 : IAudio
            where T2 : ISource
        {
            foreach (var clipPack in clipPacks)
            {
                LoadAudioClip(clipPack.Item1, clipPack.Item2);
            }
        }

        public void StopAudioSource<T>(T sourceEnum, bool output = false)
            where T : ISource
        {
            if (_sourceStates.TryGetValue(sourceEnum.Value, out var state))
            {
                if (_registeredAudioSources.TryGetValue(sourceEnum.Value, out var source))
                {
                    CancelRewarmTask(sourceEnum.Value);

                    if (source.isPlaying)
                    {
                        source.Stop();
                    }

                    state.PendingClipName = null;

                    if (state.WarmedUpClip != null)
                    {
                        source.clip = state.WarmedUpClip;
                        source.volume = 0f;
                        source.time = 0f;
                        source.Play();
                        source.Pause();

                        LogManager.Log(
                            $"停止并重预热: {sourceEnum.Value}",
                            nameof(AudioManager),
                            output
                        );
                    }
                }
            }
        }

        public bool IsAudioSourcePlaying<T>(T sourceEnum)
            where T : ISource
        {
            if (_registeredAudioSources.TryGetValue(sourceEnum.Value, out var source))
            {
                return source.isPlaying;
            }
            return false;
        }

        public AudioClip GetCurrentClip<T>(T sourceEnum)
            where T : ISource
        {
            if (_sourceStates.TryGetValue(sourceEnum.Value, out var state))
            {
                return state.CurrentClip;
            }
            return null;
        }

        public async UniTask WaitForAudioReady<T>(T sourceEnum)
            where T : ISource
        {
            if (_sourceStates.TryGetValue(sourceEnum.Value, out var state))
            {
                while (state.IsProcessing)
                {
                    await UniTask.DelayFrame(1);
                }
            }
        }

        private void CancelRewarmTask(string sourceEnum)
        {
            if (_sourceStates.TryGetValue(sourceEnum, out var state))
            {
                state.RewarmToken.Cancel();
                state.RewarmToken.Dispose();
                state.RewarmToken = new CancellationTokenSource();
            }
        }
    }
}
