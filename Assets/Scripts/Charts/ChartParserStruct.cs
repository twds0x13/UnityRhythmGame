using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

// using static ChartReader;

namespace ChartParser
{
    /// <summary>
    /// 谱面文件类型
    /// </summary>
    public enum ChartFileType
    {
        Chart, // .mc 文件
        Audio, // 音频文件
        Cover, // 封面图片
        Other, // 其他文件
    }

    /// <summary>
    /// 压缩包内的文件项
    /// </summary>
    public class ZipFileEntry
    {
        public string FileName { get; set; }
        public string RelativePath { get; set; }
        public ChartFileType FileType { get; set; }
        public long Size { get; set; }
        public byte[] Data { get; set; }

        public string Extension => Path.GetExtension(FileName).ToLowerInvariant();
    }

    /// <summary>
    /// BPM变化点
    /// </summary>
    public class BPMChange
    {
        public int Measure { get; set; } // 小节
        public int Beat { get; set; } // 拍
        public int Division { get; set; } // 分度
        public double BPM { get; set; } // BPM值
    }

    /// <summary>
    /// 音符信息
    /// </summary>
    public class Note
    {
        public int Measure { get; set; } // 小节
        public int Beat { get; set; } // 拍
        public int Division { get; set; } // 分度
        public int TrackNum { get; set; } // 轨道
        public bool IsHold { get; set; } // 是否是Hold音符
        public int EndMeasure { get; set; } // 结束小节（Hold用）
        public int EndBeat { get; set; } // 结束拍（Hold用）
        public int EndDivision { get; set; } // 结束分度（Hold用）
        public string SoundFile { get; set; } // 音效文件（Key音用）
        public int Volume { get; set; } = 100; // 音量
    }

    /// <summary>
    /// 谱面数据（对应 .mc 文件）
    /// </summary>
    public class Chart
    {
        // 原有属性
        public string ChartId { get; set; }
        public string ChartName { get; set; }
        public string Artist { get; set; }
        public string Charter { get; set; }
        public string AudioFileName { get; set; }
        public string CoverFileName { get; set; }
        public double BPM { get; set; }
        public double Duration { get; set; }
        public int Difficulty { get; set; }
        public JObject RawData { get; set; }

        // 新增属性
        public string BackgroundFileName { get; set; }
        public string Version { get; set; }
        public string SkinFileName { get; set; }
        public int PreviewTime { get; set; }
        public string VideoFileName { get; set; }
        public int GameMode { get; set; }
        public long LastModifiedTime { get; set; }
        public int SongId { get; set; }
        public string OriginalTitle { get; set; }
        public string OriginalArtist { get; set; }
        public int ColumnCount { get; set; }
        public int BarBegin { get; set; }
        public int AudioOffset { get; set; }
        public int AudioVolume { get; set; } = 100;
        public int Divide { get; set; } = 4;
        public int PlaySpeed { get; set; } = 100;
        public int EditMode { get; set; }

        // 集合属性
        public List<BPMChange> BPMChanges { get; set; } = new List<BPMChange>();
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    /// <summary>
    /// 谱面文件包（对应 .mcz 文件）
    /// </summary>
    public class ChartFile
    {
        public string FilePath { get; set; }
        public string FileName => Path.GetFileName(FilePath);
        public string Name => Path.GetFileNameWithoutExtension(FilePath);
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }

        // 包内包含的所有谱面
        public List<Chart> Charts { get; set; } = new List<Chart>();

        // 包内所有文件
        public List<ZipFileEntry> AllFiles { get; set; } = new List<ZipFileEntry>();
    }

    /// <summary>
    /// 谱面集合
    /// </summary>
    public class ChartCollection
    {
        public List<ChartFile> ChartFiles { get; set; } = new List<ChartFile>();
        public int TotalCharts => ChartFiles.Sum(f => f.Charts.Count);

        // 便捷方法
        public IEnumerable<Chart> GetAllCharts() => ChartFiles.SelectMany(f => f.Charts);
    }
}
