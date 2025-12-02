using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Parser;
using Chart = JsonLoader.ChartJsonLoader;

/*
public class ChartReader
{
    private JObject chart;
    private List<BPMChange> bpmTimeline;
    private Queue<Note>[] trackQueues;
    private List<Note>[] trackArchives; // 轨道存档
    private List<Note> allNotesArchive; // 所有音符的存档

    public double NoteFallDuration { get; set; } = 1000;
    public int TrackCount => trackQueues?.Length ?? 0;

    public void ParseChart()
    {
        (_, chart) = Chart.LoadObject<JObject>("test.mcz", "1599569663.mc");

        ParseBPMChanges();
        BuildTimeline();
        InitializeTrackQueues();
        CreateArchives(); // 创建存档
    }

    private void ParseBPMChanges()
    {
        bpmTimeline = new List<BPMChange>();
        var timeArray = chart["time"] as JArray;
        if (timeArray == null)
            return;

        foreach (var item in timeArray)
        {
            var beatArray = item["beat"] as JArray;
            if (beatArray?.Count != 3)
                continue;

            double beat = ConvertBeatToDouble(beatArray);
            double bpm = item["bpm"]?.ToObject<double>() ?? 0;
            bpmTimeline.Add(new BPMChange { Beat = beat, BPM = bpm });
        }

        bpmTimeline = bpmTimeline.OrderBy(b => b.Beat).ToList();
    }

    private double ConvertBeatToDouble(JArray beatArray)
    {
        int measure = beatArray[0].ToObject<int>();
        int numerator = beatArray[1].ToObject<int>();
        int denominator = beatArray[2].ToObject<int>();
        return measure + (double)numerator / denominator;
    }

    private void BuildTimeline()
    {
        if (bpmTimeline.Count == 0)
            return;

        if (bpmTimeline[0].Beat > 0)
        {
            bpmTimeline.Insert(0, new BPMChange { Beat = 0, BPM = bpmTimeline[0].BPM });
        }

        double currentTime = 0;
        for (int i = 0; i < bpmTimeline.Count - 1; i++)
        {
            var current = bpmTimeline[i];
            var next = bpmTimeline[i + 1];

            double beatDiff = next.Beat - current.Beat;
            double timeDiff = beatDiff * (60000.0 / current.BPM);

            current.Time = currentTime;
            currentTime += timeDiff;
            bpmTimeline[i + 1].Time = currentTime;
        }

        bpmTimeline[bpmTimeline.Count - 1].Time = currentTime;
    }

    private double ConvertBeatToTime(double beat)
    {
        if (bpmTimeline == null || bpmTimeline.Count == 0)
            return 0;

        var lastChange = bpmTimeline.LastOrDefault(b => b.Beat <= beat);
        if (lastChange == null)
            return 0;

        double beatDiff = beat - lastChange.Beat;
        return lastChange.Time + beatDiff * (60000.0 / lastChange.BPM);
    }

    /// <summary>
    /// 初始化轨道队列
    /// </summary>
    private void InitializeTrackQueues()
    {
        int trackCount = 4; // 可以根据实际需要调整轨道数量
        trackQueues = new Queue<Note>[trackCount];
        for (int i = 0; i < trackCount; i++)
            trackQueues[i] = new Queue<Note>();

        var notes = ParseNotes();
        foreach (var note in notes)
        {
            if (note.TrackNum >= 0 && note.TrackNum < trackCount)
                trackQueues[note.TrackNum].Enqueue(note);
        }
    }

    /// <summary>
    /// 创建队列存档
    /// </summary>
    private void CreateArchives()
    {
        if (trackQueues == null)
            return;

        int trackCount = trackQueues.Length;
        trackArchives = new List<Note>[trackCount];
        allNotesArchive = new List<Note>();

        for (int i = 0; i < trackCount; i++)
        {
            trackArchives[i] = new List<Note>(trackQueues[i]);
            allNotesArchive.AddRange(trackQueues[i]);
        }

        // 按生成时间排序所有音符存档
        allNotesArchive = allNotesArchive.OrderBy(n => n.GenerateTime).ToList();
    }

    private List<Note> ParseNotes()
    {
        var notes = new List<Note>();
        var noteArray = chart["note"] as JArray;
        if (noteArray == null)
            return notes;

        foreach (var item in noteArray)
        {
            if (item["type"]?.ToObject<int>() == 1)
                continue;

            var beatArray = item["beat"] as JArray;
            if (beatArray?.Count != 3)
                continue;

            double beat = ConvertBeatToDouble(beatArray);
            int column = item["column"]?.ToObject<int>() ?? 0;

            double hitTime = ConvertBeatToTime(beat);
            double generateTime = hitTime - NoteFallDuration;

            var note = new Note
            {
                TrackNum = column,
                HitTime = hitTime,
                GenerateTime = generateTime,
                IsHold = false,
                Beat = beat,
            };

            var endBeatArray = item["endbeat"] as JArray;
            if (endBeatArray?.Count == 3)
            {
                double endBeat = ConvertBeatToDouble(endBeatArray);
                note.IsHold = true;
                note.EndHitTime = ConvertBeatToTime(endBeat);
                note.EndBeat = endBeat;
            }

            notes.Add(note);
        }

        return notes.OrderBy(n => n.GenerateTime).ToList();
    }

    /// <summary>
    /// 获取指定轨道的音符队列（副本）
    /// </summary>
    public Queue<Note> GetTrackQueue(int track)
    {
        if (track < 0 || track >= TrackCount)
            throw new ArgumentOutOfRangeException(
                nameof(track),
                $"轨道索引必须在0-{TrackCount - 1}范围内"
            );

        return new Queue<Note>(trackQueues[track]);
    }

    /// <summary>
    /// 获取指定轨道的队列存档（只读）
    /// </summary>
    public IReadOnlyList<Note> GetTrackArchive(int track)
    {
        if (track < 0 || track >= TrackCount)
            throw new ArgumentOutOfRangeException(
                nameof(track),
                $"轨道索引必须在0-{TrackCount - 1}范围内"
            );

        return trackArchives?[track]?.AsReadOnly() ?? new List<Note>().AsReadOnly();
    }

    /// <summary>
    /// 获取所有音符的存档（只读）
    /// </summary>
    public IReadOnlyList<Note> GetAllNotesArchive()
    {
        return allNotesArchive?.AsReadOnly() ?? new List<Note>().AsReadOnly();
    }

    /// <summary>
    /// 获取所有需要生成的音符（当前时间之前）
    /// </summary>
    public List<Note> GetNotesToGenerate(double currentTime)
    {
        var notes = new List<Note>();

        for (int i = 0; i < TrackCount; i++)
        {
            while (trackQueues[i].Count > 0 && trackQueues[i].Peek().GenerateTime <= currentTime)
            {
                notes.Add(trackQueues[i].Dequeue());
            }
        }

        return notes.OrderBy(n => n.GenerateTime).ToList();
    }

    /// <summary>
    /// 获取所有轨道的队列副本（用于批量操作）
    /// </summary>
    public Queue<Note>[] GetAllTrackQueues()
    {
        var queues = new Queue<Note>[TrackCount];
        for (int i = 0; i < TrackCount; i++)
        {
            queues[i] = new Queue<Note>(trackQueues[i]);
        }
        return queues;
    }

    /// <summary>
    /// 重置指定轨道的队列到初始状态
    /// </summary>
    public void ResetTrackQueue(int track)
    {
        if (track < 0 || track >= TrackCount)
            throw new ArgumentOutOfRangeException(
                nameof(track),
                $"轨道索引必须在0-{TrackCount - 1}范围内"
            );

        if (trackArchives?[track] != null)
        {
            trackQueues[track] = new Queue<Note>(trackArchives[track]);
        }
    }

    /// <summary>
    /// 重置所有轨道队列到初始状态
    /// </summary>
    public void ResetAllTrackQueues()
    {
        for (int i = 0; i < TrackCount; i++)
        {
            ResetTrackQueue(i);
        }
    }

    /// <summary>
    /// 获取轨道数量
    /// </summary>
    public int GetTrackCount()
    {
        return TrackCount;
    }

    /// <summary>
    /// 检查是否所有轨道的队列都已为空
    /// </summary>
    public bool AllQueuesEmpty()
    {
        return trackQueues?.All(queue => queue.Count == 0) ?? true;
    }

    /// <summary>
    /// 获取下一个即将生成的音符（不弹出队列）
    /// </summary>
    public Note PeekNextNoteToGenerate()
    {
        Note nextNote = null;
        double minTime = double.MaxValue;

        for (int i = 0; i < TrackCount; i++)
        {
            if (trackQueues[i].Count > 0)
            {
                var note = trackQueues[i].Peek();
                if (note.GenerateTime < minTime)
                {
                    minTime = note.GenerateTime;
                    nextNote = note;
                }
            }
        }

        return nextNote;
    }

}
*/
