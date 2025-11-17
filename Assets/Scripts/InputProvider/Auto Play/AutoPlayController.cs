using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameManagerNS;
using Parser;
using UnityEngine;

public class AutoPlayController : IAutoPlayController, IDisposable
{
    public event Action<int> OnTrackShouldPress;
    public event Action<int> OnTrackShouldRelease;

    private readonly ChartManager _chartManager;
    private readonly Dictionary<int, List<NoteEvent>> _scheduledEvents;
    private CancellationTokenSource _autoPlayCancellationTokenSource;
    private float _gameTimeStart;
    private bool _isRegistered = false;

    public AutoPlayController(ChartManager chartManager)
    {
        _chartManager = chartManager;
        _scheduledEvents = new Dictionary<int, List<NoteEvent>>();
        _autoPlayCancellationTokenSource = new CancellationTokenSource();
    }

    public void RegisterToGameStart()
    {
        if (_isRegistered)
            return;

        // 注册到游戏开始事件
        _chartManager.OnStartGame += OnGameStart;
        _chartManager.OnExitGame += OnGameExit;
        _chartManager.OnStartNoteGeneration += OnStartNoteGeneration;

        _isRegistered = true;
        LogManager.Log("AutoPlayController registered to game events", nameof(AutoPlayController));
    }

    public void UnregisterFromGameStart()
    {
        if (!_isRegistered)
            return;

        // 取消注册
        _chartManager.OnStartGame -= OnGameStart;
        _chartManager.OnExitGame -= OnGameExit;
        _chartManager.OnStartNoteGeneration -= OnStartNoteGeneration;

        // 停止自动播放
        StopAutoPlay();

        _isRegistered = false;
        LogManager.Log(
            "AutoPlayController unregistered from game events",
            nameof(AutoPlayController)
        );
    }

    private void OnGameStart()
    {
        // 游戏开始时清除之前的事件
        _scheduledEvents.Clear();
        LogManager.Log(
            "Game started, clearing previous auto-play events",
            nameof(AutoPlayController)
        );
    }

    private void OnGameExit()
    {
        StopAutoPlay();
    }

    private void OnStartNoteGeneration()
    {
        // 开始音符生成时，启动自动播放
        StartAutoPlay().Forget();
    }

    private async UniTaskVoid StartAutoPlay()
    {
        if (
            _chartManager.SelectedChart?.Notes == null
            || _chartManager.SelectedChart.Notes.Count == 0
        )
        {
            LogManager.Log(
                "No chart selected or no notes available for auto-play",
                nameof(AutoPlayController)
            );
            return;
        }

        try
        {
            // 记录游戏时间起点（与ChartManager中的gameTimeStart同步）
            _gameTimeStart = GameManager.Inst.GetGameTime();

            // 计算音符的真实判定时间并安排事件
            await ScheduleNoteEvents(_chartManager.SelectedChart.Notes);

            // 开始执行自动播放
            await ExecuteAutoPlay(_autoPlayCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            LogManager.Log("Auto-play cancelled", nameof(AutoPlayController), false);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Auto-play error: {ex.Message}");
        }
    }

    private async UniTask ScheduleNoteEvents(List<Note> notes)
    {
        // 下落时间（与ChartManager中的fallDuration保持一致）
        float fallDuration = GameSettings.NoteFallDuration;

        // 在后台线程中处理音符分组和事件安排
        await UniTask.RunOnThreadPool(() =>
        {
            lock (_scheduledEvents)
            {
                _scheduledEvents.Clear();

                // 按轨道分组音符
                var trackGroups = notes.GroupBy(n => n.TrackNum);

                foreach (var trackGroup in trackGroups)
                {
                    int trackNumber = trackGroup.Key;
                    var noteEvents = new List<NoteEvent>();

                    foreach (var note in trackGroup.OrderBy(n => n.Time))
                    {
                        // 计算真实的判定时间
                        // 判定时间 = note.Time + _gameTimeStart
                        float judgmentTime = note.Time + _gameTimeStart;

                        if (!note.IsHold)
                        {
                            // 单键音符：在判定时间触发按下，短暂延迟后触发释放
                            noteEvents.Add(
                                new NoteEvent
                                {
                                    Time = judgmentTime,
                                    EventType = NoteEventType.Press,
                                    TrackNumber = trackNumber,
                                }
                            );

                            // 短按持续时间（ 33.3333 ms 后释放 ）

                            // 如果谱面要求单指 kps 超过了 30 那应该是谱的问题

                            noteEvents.Add(
                                new NoteEvent
                                {
                                    Time = judgmentTime + 0.0333333f,
                                    EventType = NoteEventType.Release,
                                    TrackNumber = trackNumber,
                                }
                            );
                        }
                        else
                        {
                            // 长按音符：在开始时间按下，在结束时间释放
                            float endJudgmentTime = note.EndTime + _gameTimeStart;

                            noteEvents.Add(
                                new NoteEvent
                                {
                                    Time = judgmentTime,
                                    EventType = NoteEventType.Press,
                                    TrackNumber = trackNumber,
                                }
                            );

                            noteEvents.Add(
                                new NoteEvent
                                {
                                    Time = endJudgmentTime,
                                    EventType = NoteEventType.Release,
                                    TrackNumber = trackNumber,
                                }
                            );
                        }
                    }

                    _scheduledEvents[trackNumber] = noteEvents.OrderBy(e => e.Time).ToList();
                }
            }
        });

        LogManager.Log($"Scheduled {notes.Count} notes for auto-play", nameof(AutoPlayController));
    }

    private async UniTask ExecuteAutoPlay(CancellationToken cancellationToken)
    {
        // 将所有事件合并并按时间排序
        var allEvents = new List<NoteEvent>();

        lock (_scheduledEvents)
        {
            foreach (var trackEvents in _scheduledEvents.Values)
            {
                allEvents.AddRange(trackEvents);
            }
        }

        var sortedEvents = allEvents.OrderBy(e => e.Time).ToList();

        LogManager.Log(
            $"Starting auto-play with {sortedEvents.Count} events",
            nameof(AutoPlayController)
        );

        foreach (var noteEvent in sortedEvents)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // 使用 WaitUntil 等待条件满足
            try
            {
                await UniTask.WaitUntil(
                    () =>
                        GameManager.Inst.GetGameTime() >= noteEvent.Time
                        || cancellationToken.IsCancellationRequested,
                    cancellationToken: cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested)
                continue;

            // 触发事件
            switch (noteEvent.EventType)
            {
                case NoteEventType.Press:
                    OnTrackShouldPress?.Invoke(noteEvent.TrackNumber);
                    LogManager.Log(
                        $"Auto-play: Track {noteEvent.TrackNumber} PRESS at time {noteEvent.Time:F3}",
                        nameof(AutoPlayController),
                        false
                    );
                    break;
                case NoteEventType.Release:
                    OnTrackShouldRelease?.Invoke(noteEvent.TrackNumber);
                    LogManager.Log(
                        $"Auto-play: Track {noteEvent.TrackNumber} RELEASE at time {noteEvent.Time:F3}",
                        nameof(AutoPlayController),
                        false
                    );
                    break;
            }
        }

        LogManager.Log("Auto-play completed", nameof(AutoPlayController));
    }

    public void StopAutoPlay()
    {
        _autoPlayCancellationTokenSource?.Cancel();
        _autoPlayCancellationTokenSource?.Dispose();
        _autoPlayCancellationTokenSource = new CancellationTokenSource();

        lock (_scheduledEvents)
        {
            _scheduledEvents.Clear();
        }

        LogManager.Log("Auto-play stopped", nameof(AutoPlayController));
    }

    public void Dispose()
    {
        UnregisterFromGameStart();
        _autoPlayCancellationTokenSource?.Cancel();
        _autoPlayCancellationTokenSource?.Dispose();
    }

    private struct NoteEvent
    {
        public float Time;
        public NoteEventType EventType;
        public int TrackNumber;
    }

    private enum NoteEventType
    {
        Press,
        Release,
    }
}
