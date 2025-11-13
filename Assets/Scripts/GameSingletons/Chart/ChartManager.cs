using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AudioNS;
using Cysharp.Threading.Tasks;
using GameManagerNS;
using NoteNS;
using Parser;
using PooledObjectNS;
using Singleton;
using UnityEngine;
using UnityEngine.Events;

public class ChartManager : Singleton<ChartManager>
{
    public ChartParser Parser;

    public ChartCollection Collection;

    public List<Chart> AllCharts;

    public Chart SelectedChart;

    public AudioClip SelectedAudio;

    public UnityEvent OnInitialized;

    // 添加 CancellationTokenSource 用于控制任务取消
    private CancellationTokenSource _gameCancellationTokenSource;

    protected override void SingletonAwake()
    {
        var path = Path.Combine(Application.streamingAssetsPath);

        var config = new ChartParser.ParserConfig { EnableLogging = true };

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

    public void StartGame(float time)
    {
        // 取消之前可能还在运行的任务
        _gameCancellationTokenSource?.Cancel();
        _gameCancellationTokenSource?.Dispose();

        // 创建新的 CancellationTokenSource
        _gameCancellationTokenSource = new CancellationTokenSource();

        StartGameTask(time, _gameCancellationTokenSource.Token).Forget();

        LogManager.Log(
            $"Game Started,Chart Offset:{SelectedChart.AudioOffset}",
            nameof(ChartManager),
            true
        );
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

            // 下落时间
            float fallDuration = 1.2f;

            float verticalPosition = 1f;

            PooledObjectManager.Inst.NoteUpdateModifier += ModifyNote;

            // 获取目标帧率（用于计算帧间隔）
            float targetFrameRate =
                Application.targetFrameRate <= 0 ? 60 : Application.targetFrameRate;

            float frameInterval = 1f / targetFrameRate;

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

                float generateTime = noteTime - fallDuration - frameInterval + gameTimeStart;

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

                    // 使用PooledObjectManager.Inst的GetNotesDynamic方法生成音符
                    PooledObjectManager.Inst.GetNotesDynamic(
                        generateTime, // StartTime
                        verticalPosition, // Vertical
                        note.TrackNum, // TrackNum
                        fallDuration // Duration
                    );

                    LogManager.Log(
                        $"Generated Note at Time: {note.Time}, Track: {note.TrackNum}",
                        nameof(ChartManager),
                        false
                    );

                    LogManager.Log(
                        $"Note : [{note.Measure},{note.Beat},{note.Division}]",
                        nameof(ChartManager),
                        false
                    );
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 任务被取消，正常退出
            LogManager.Log("Chart task cancelled", nameof(ChartManager), false);
        }
    }

    private void ModifyNote(NoteBehaviour note)
    {
        /* First Pattern !
        if (note.AnimeMachine.CurT < 0.45f)
        {
            note.SpriteRenderer.color = new Color(1f, 1f, 1f, 0.2f * note.AnimeMachine.CurT);
            note.SetVertical(3f * note.AnimeMachine.CurT);
        }
        else
        {
            note.SpriteRenderer.color = new Color(1f, 1f, 1f, 0.6f + 0.4f * note.AnimeMachine.CurT);
            note.SetVertical(3f * note.AnimeMachine.CurT);
        }
        */

        note.SetVertical(1.5f);
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

        PooledObjectManager.Inst.NoteUpdateModifier -= ModifyNote;

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
