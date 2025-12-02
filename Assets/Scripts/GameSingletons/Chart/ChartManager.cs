using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AudioNS;
using Cysharp.Threading.Tasks;
using GameManagerNS;
using Parser;
using PooledObjectNS;
using Singleton;
using UnityEngine;
using UnityEngine.Events;
using Pool = PooledObjectNS.PooledObjectManager;

public class ChartManager : Singleton<ChartManager>
{
    public ChartParser Parser;

    public ChartCollection Collection;

    public List<Chart> AllCharts;

    public Chart SelectedChart;

    public AudioClip SelectedAudio;

    public UnityEvent OnInitialized;

    public Action OnStartGame; // 代表游戏开始加载，开始加载谱面和音频的一瞬间

    public Action OnExitGame; // 代表游戏 Note 生成和音频 Task 都恰好完成的一瞬间

    public Action OnStartNoteGeneration; // 代表游戏完成加载，开始生成 Note 的一瞬间

    // 添加 CancellationTokenSource 用于控制任务取消
    private CancellationTokenSource _gameCancellationTokenSource;

    protected override void SingletonAwake()
    {
        var path = Path.Combine(Application.streamingAssetsPath);

        var config = new ChartParser.ParserConfig { EnableLogging = false };

        Parser = new ChartParser(path, config);

        ParseCharts().Forget();
    }

    private async UniTaskVoid ParseCharts()
    {
        Collection = await Parser.ScanAsync();

        AllCharts = Parser.GetAllCharts();

        OnInitialized?.Invoke();
    }

    public bool SelectChart(string chartName)
    {
        var chart = AllCharts.FirstOrDefault(c => c.ChartName == chartName);

        if (chart != null)
        {
            SelectedChart = chart;

            return true;
        }

        return false;
    }

    public bool SelectChart(int index)
    {
        if (index >= 0 && index < AllCharts.Count)
        {
            SelectedChart = AllCharts[index];

            return true;
        }

        return false;
    }

    public void StartGame()
    {
        var time = GameManager.Inst.GetGameTime();

        // 取消之前可能还在运行的任务
        _gameCancellationTokenSource?.Cancel();
        _gameCancellationTokenSource?.Dispose();

        // 创建新的 CancellationTokenSource
        _gameCancellationTokenSource = new CancellationTokenSource();

        OnStartGame?.Invoke();

        StartGameTask(time, _gameCancellationTokenSource.Token).Forget();

        LogManager.Log(
            $"Game Started,Chart Offset:{SelectedChart.AudioOffset}",
            nameof(ChartManager),
            true
        );
    }

    /// <summary>
    /// Game Settings页面内预览游戏下落设置
    /// </summary>
    public void StartPreviewSettings()
    {
        var time = GameManager.Inst.GetGameTime();

        _gameCancellationTokenSource?.Cancel();
        _gameCancellationTokenSource?.Dispose();

        _gameCancellationTokenSource = new CancellationTokenSource();

        StartPreviewTask(_gameCancellationTokenSource.Token).Forget();
    }

    protected async UniTask StartPreviewTask(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            // 注册垂直修改器
            Pool.Inst.VerticalModifier += VerticalModify;

            LogManager.Log("开始预览模式，每秒在3号轨道生成一个音符", nameof(ChartManager), true);

            // 记录开始时间
            var startTime = GameManager.Inst.GetGameTime();

            // 发送开始生成音符事件
            OnStartNoteGeneration?.Invoke();

            // 循环生成预览音符，直到任务被取消
            while (!cancellationToken.IsCancellationRequested)
            {
                // 计算当前时间偏移
                var currentTimeOffset = GameManager.Inst.GetGameTime() - startTime;

                // 计算生成时间（当前时间 + 下落时间）
                var generateTime = GameManager.Inst.GetGameTime();

                // 在3号轨道生成单个音符
                Pool.Inst.GetNotesDynamic(
                    generateTime, // StartTime
                    GameSettings.ChartVerticalScale, // Vertical
                    3, // TrackNum (0号轨道)
                    GameSettings.NoteFallDuration // Duration
                );

                LogManager.Log(
                    $"生成预览音符 - 时间: {generateTime:F2}, 轨道: 3, 下落时间: {GameSettings.NoteFallDuration:F2}",
                    nameof(ChartManager),
                    false
                );

                // 等待1秒，同时支持取消
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 任务被取消，正常退出
            LogManager.Log("预览任务被取消", nameof(ChartManager), false);
        }
        finally
        {
            // 清理垂直修改器
            Pool.Inst.VerticalModifier -= VerticalModify;

            LogManager.Log("预览模式结束", nameof(ChartManager), true);
        }
    }

    protected async UniTask StartGameTask(
        float startTime,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var chartTask = StartChartTask(startTime, cancellationToken);
        var audioTask = StartAudioTask(startTime, cancellationToken);

        // 等待两个任务完成或取消
        await UniTask.WhenAll(chartTask, audioTask).SuppressCancellationThrow();

        OnExitGame?.Invoke();
    }

    protected async UniTask StartAudioTask(
        float startTime,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (SelectedChart?.Notes is null || SelectedChart.Notes.Count == 0)
            return;

        try
        {
            // 使用带取消令牌的延迟
            await UniTask.Delay(600, cancellationToken: cancellationToken);

            SelectedAudio = await Parser.GetAudioClipAsync(SelectedChart);

            if (cancellationToken.IsCancellationRequested)
                return;

            await AudioManager.Inst.PrewarmAudioClipAsync(SelectedAudio, Source.BGM);

            var loadTime = GameManager.Inst.GetGameTime() - startTime;

            var delayTime = 1200 + SelectedChart.AudioOffset - (int)(loadTime * 1000);

            LogManager.Log(delayTime.ToString(), nameof(ChartManager));

            if (delayTime > 0)
            {
                await UniTask.Delay(delayTime, cancellationToken: cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                AudioManager.Inst.PlayAudioClip(SelectedAudio, Source.BGM);
            }
        }
        catch (OperationCanceledException)
        {
            // 任务被取消，正常退出
            LogManager.Log("Audio task cancelled", nameof(ChartManager), false);
        }
    }

    protected async UniTask StartChartTask(
        float startTime,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (SelectedChart?.Notes == null || SelectedChart.Notes.Count == 0)
            return;

        try
        {
            await UniTask.Delay(600, cancellationToken: cancellationToken);

            Pool.Inst.VerticalModifier += VerticalModify;

            // 在后台线程中分组音符
            var noteGroups = await GroupNotesOnThreadAsync(SelectedChart.Notes);

            if (cancellationToken.IsCancellationRequested)
                return;

            var loadTime = GameManager.Inst.GetGameTime() - startTime;

            var delayTime = 1200 - (int)(loadTime * 1000);

            LogManager.Log(delayTime.ToString(), nameof(ChartManager));

            if (delayTime > 0)
            {
                await UniTask.Delay(delayTime, cancellationToken: cancellationToken);
            }

            // 游戏时间起点
            var gameTimeStart = GameManager.Inst.GetGameTime();

            // 发送事件
            OnStartNoteGeneration?.Invoke();

            // 生成音符
            foreach (var group in noteGroups)
            {
                // 每次循环开始都检查取消令牌
                if (cancellationToken.IsCancellationRequested)
                    break;

                float noteTime = group.Key;
                var notesInGroup = group.Value;

                // 计算生成时间（ 音符判定时间 - 下落时间 - 一帧的时间 + 游戏时间起点 ）

                // 这里的游戏时间起点自动包含了前面的延迟时间

                float generateTime = noteTime - GameSettings.NoteFallDuration + gameTimeStart;

                // 等待到调整后的生成时间，同时检查取消
                while (GameManager.Inst.GetGameTime() < generateTime)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await UniTask.Yield(cancellationToken);
                }

                // 如果游戏暂停或被取消则跳过
                if (GameManager.Inst.IsGamePaused() || cancellationToken.IsCancellationRequested)
                    continue;

                Rect rect = ResizeDetector.Inst.Rect.rect;

                // 同时生成这一组的所有音符
                foreach (var note in notesInGroup)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (!note.IsHold)
                    {
                        Pool.Inst.GetNotesDynamic(
                            generateTime, // StartTime
                            GameSettings.ChartVerticalScale, // Vertical
                            note.TrackNum, // TrackNum
                            GameSettings.NoteFallDuration // Duration
                        );
                    }
                    else
                    {
                        float finishTime =
                            note.EndTime - GameSettings.NoteFallDuration + gameTimeStart;

                        Pool.Inst.GetHoldsDynamic(
                            generateTime, // StartTime
                            finishTime, // EndTime
                            GameSettings.ChartVerticalScale, // Vertical
                            note.TrackNum, // TrackNum
                            GameSettings.NoteFallDuration // Duration
                        );

                        LogManager.Log(
                            $"Generated Hold at Time: {note.Time} , {note.EndTime} , Track: {note.TrackNum}, fallDuration : {GameSettings.NoteFallDuration}",
                            nameof(ChartManager),
                            false
                        );

                        LogManager.Log(
                            $"Hold : [{note.Measure},{note.Beat},{note.Division}] [{note.EndMeasure},{note.EndBeat},{note.EndDivision}]",
                            nameof(ChartManager),
                            false
                        );
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 任务被取消，正常退出
            LogManager.Log("Chart task cancelled", nameof(ChartManager), false);
        }
    }

    /// <summary>
    /// 可以在运行时注册事件，根据查找条件调整特定 Note 的 Vertical 值（以下为框架示例）
    /// </summary>
    /// <param name="vertical"></param>
    private void VerticalModify(IVertical vertical)
    {
        vertical.Vertical = 0.8f;
    }

    /// <summary>
    /// 在后台线程中将音符按时间分组，处理多押情况
    /// </summary>
    private async UniTask<Dictionary<float, List<Note>>> GroupNotesOnThreadAsync(List<Note> notes)
    {
        return await UniTask.RunOnThreadPool(() =>
        {
            var noteGroups = new Dictionary<float, List<Note>>();
            const float timeTolerance = 0.001f;

            // 分组音符
            foreach (var note in notes)
            {
                bool added = false;
                foreach (var existingTime in noteGroups.Keys)
                {
                    if (Math.Abs(note.Time - existingTime) < timeTolerance)
                    {
                        noteGroups[existingTime].Add(note);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    noteGroups[note.Time] = new List<Note> { note };
                }
            }

            // 按时间排序
            return noteGroups.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        });
    }

    public void ExitGame()
    {
        // 取消所有正在运行的游戏任务
        _gameCancellationTokenSource?.Cancel();

        OnExitGame?.Invoke();

        Pool.Inst.VerticalModifier -= VerticalModify;

        AudioManager.Inst.PauseAudioSource(Source.BGM);
        AudioManager.Inst.ClearAudioSource(Source.BGM);
    }

    protected override void SingletonDestroy()
    {
        // 取消并释放 CancellationTokenSource
        _gameCancellationTokenSource?.Cancel();
        _gameCancellationTokenSource?.Dispose();
        _gameCancellationTokenSource = null;

        Parser?.Dispose();
    }
}
