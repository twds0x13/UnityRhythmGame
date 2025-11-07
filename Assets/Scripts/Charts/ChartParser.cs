using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

// using static ChartReader;

namespace ChartParser
{
    /// <summary>
    /// 谱面文件解析器
    /// </summary>
    public sealed class ChartParser : IDisposable
    {
        #region 配置

        public class ParserConfig
        {
            public string[] ChartExtensions { get; set; } = { ".mc", ".json" };
            public string ArchiveExtension { get; set; } = ".mcz";
            public bool EnableLogging { get; set; } = true;
        }

        #endregion

        #region 字段和属性

        private readonly ParserConfig _config;
        private bool _disposed = false;

        public string ScanDirectory { get; private set; }
        public ChartCollection ChartCollection { get; private set; } = new ChartCollection();

        #endregion

        #region 构造函数

        public ChartParser(string scanDirectory, ParserConfig config = null)
        {
            ScanDirectory = scanDirectory ?? throw new ArgumentNullException(nameof(scanDirectory));
            _config = config ?? new ParserConfig();

            LogManager.Log(
                $"创建 ChartParser 实例，扫描目录: {ScanDirectory}",
                nameof(ChartParser),
                _config.EnableLogging
            );

            if (!Directory.Exists(ScanDirectory))
            {
                LogManager.Warning(
                    $"目录不存在，创建目录: {ScanDirectory}",
                    nameof(ChartParser),
                    _config.EnableLogging
                );
                Directory.CreateDirectory(ScanDirectory);
            }
        }

        #endregion

        #region 扫描方法

        /// <summary>
        /// 扫描目录中的谱面文件
        /// </summary>
        public async Task<ChartCollection> ScanAsync(
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                logger.Info($"开始扫描目录: {ScanDirectory}");

                var chartFiles = new List<ChartFile>();
                var archiveFiles = Directory.GetFiles(
                    ScanDirectory,
                    $"*{_config.ArchiveExtension}",
                    SearchOption.AllDirectories
                );

                logger.Info($"找到 {archiveFiles.Length} 个谱面文件");

                foreach (var filePath in archiveFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var chartFile = await Task.Run(
                            () => ParseChartFile(filePath, logger),
                            cancellationToken
                        );
                        if (chartFile != null)
                        {
                            chartFiles.Add(chartFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"处理文件时出错: {filePath} - {ex.Message}");
                    }
                }

                ChartCollection = new ChartCollection { ChartFiles = chartFiles };
                logger.Info(
                    $"扫描完成，共加载 {chartFiles.Count} 个谱面文件，包含 {ChartCollection.TotalCharts} 个谱面"
                );

                return ChartCollection;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        #endregion

        #region 文件解析核心

        /// <summary>
        /// 解析单个谱面文件
        /// </summary>
        private ChartFile ParseChartFile(string filePath, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                logger.Log($"解析谱面文件: {filePath}");

                var fileInfo = new FileInfo(filePath);
                var chartFile = new ChartFile
                {
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                };

                using (var archive = ZipFile.OpenRead(filePath))
                {
                    // 获取所有文件条目并处理可能的 /0/ 文件夹包裹
                    var entries = archive.Entries.ToList();
                    var filesToProcess = GetFilesToProcess(entries, logger);

                    foreach (var entry in filesToProcess)
                    {
                        var fileEntry = CreateZipFileEntry(entry, logger);
                        if (fileEntry != null)
                        {
                            chartFile.AllFiles.Add(fileEntry);
                        }
                    }

                    // 解析谱面文件
                    ParseChartFiles(chartFile, logger);
                }

                logger.Info($"成功解析: {chartFile.FileName} - {chartFile.Charts.Count} 个谱面");
                return chartFile;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 确定要处理的文件列表（处理 /0/ 文件夹包裹）
        /// </summary>
        private List<ZipArchiveEntry> GetFilesToProcess(
            List<ZipArchiveEntry> entries,
            AsyncLogger logger = null
        )
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                var rootEntries = entries.Where(e => !e.FullName.Contains('/')).ToList();
                if (rootEntries.Any())
                {
                    logger.Log($"使用根目录文件，数量: {rootEntries.Count}");
                    return rootEntries;
                }

                var subfolderEntries = entries.Where(e => e.FullName.StartsWith("0/")).ToList();
                logger.Log($"使用子目录文件，数量: {subfolderEntries.Count}");
                return subfolderEntries;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 创建压缩包文件条目
        /// </summary>
        private ZipFileEntry CreateZipFileEntry(ZipArchiveEntry entry, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                if (entry.Length == 0)
                {
                    logger.Warning($"跳过空文件: {entry.FullName}");
                    return null;
                }

                var fileName = Path.GetFileName(entry.FullName);
                logger.Log($"创建文件条目: {fileName}");

                using (var stream = entry.Open())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);

                    return new ZipFileEntry
                    {
                        FileName = fileName,
                        RelativePath = entry.FullName,
                        Data = memoryStream.ToArray(),
                    };
                }
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 解析谱面文件
        /// </summary>
        private void ParseChartFiles(ChartFile chartFile, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                var chartFileEntries = chartFile
                    .AllFiles.Where(f => _config.ChartExtensions.Contains(f.Extension))
                    .ToList();

                logger.Info($"找到 {chartFileEntries.Count} 个谱面文件需要解析");

                foreach (var fileEntry in chartFileEntries)
                {
                    try
                    {
                        var chart = ParseChartData(fileEntry, logger);
                        if (chart != null)
                        {
                            chartFile.Charts.Add(chart);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"解析谱面文件失败: {fileEntry.FileName} - {ex.Message}");
                    }
                }
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 解析单个谱面数据
        /// </summary>
        private Chart ParseChartData(ZipFileEntry fileEntry, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                logger.Log($"解析谱面数据: {fileEntry.FileName}");

                var jsonText = System.Text.Encoding.UTF8.GetString(fileEntry.Data);
                var jsonObject = JObject.Parse(jsonText);

                var chart = new Chart { ChartId = Guid.NewGuid().ToString(), RawData = jsonObject };

                // 从JSON中提取谱面信息
                ExtractChartInfo(chart, jsonObject, logger);

                logger.Info($"成功解析谱面: {chart.ChartName}");
                return chart;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 从JSON数据中提取谱面信息（按照Malody格式）
        /// </summary>
        private void ExtractChartInfo(Chart chart, JObject jsonData, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                logger.Log("开始解析谱面信息");

                // 1. 解析 meta 部分 - 歌曲元数据
                var meta = jsonData["meta"] as JObject;
                if (meta != null)
                {
                    ExtractMetaInfo(chart, meta, logger);
                }
                else
                {
                    logger.Warning("未找到 meta 部分，使用默认值");
                    chart.ChartName = "未知谱面";
                    chart.Artist = "未知艺术家";
                }

                // 2. 解析 time 部分 - BPM变化信息
                var timeArray = jsonData["time"] as JArray;
                if (timeArray != null && timeArray.Count > 0)
                {
                    ExtractBPMInfo(chart, timeArray, logger);
                }
                else
                {
                    logger.Warning("未找到 time 部分，BPM使用默认值");
                    chart.BPM = 120.0; // 默认BPM
                }

                // 3. 解析 note 部分 - 音符和音频信息
                var noteArray = jsonData["note"] as JArray;
                if (noteArray != null)
                {
                    ExtractNoteInfo(chart, noteArray, logger);
                }

                // 4. 解析额外信息
                var extra = jsonData["extra"] as JObject;
                if (extra != null)
                {
                    ExtractExtraInfo(chart, extra, logger);
                }

                logger.Info($"谱面解析完成: {chart.ChartName} - {chart.Artist} - BPM: {chart.BPM}");
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 解析 meta 部分信息
        /// </summary>
        private void ExtractMetaInfo(Chart chart, JObject meta, AsyncLogger logger)
        {
            // 谱师信息
            chart.Charter = meta["creator"]?.ToString() ?? "未知谱师";

            // 背景图
            chart.BackgroundFileName = meta["background"]?.ToString();

            // 版本信息
            chart.Version = meta["version"]?.ToString();

            // 皮肤文件
            chart.SkinFileName = meta["skin"]?.ToString();

            // 预览时间（毫秒）
            if (int.TryParse(meta["preview"]?.ToString(), out int previewTime))
            {
                chart.PreviewTime = previewTime;
            }

            // 视频文件
            chart.VideoFileName = meta["video"]?.ToString();

            // 谱面ID
            if (int.TryParse(meta["id"]?.ToString(), out int chartId))
            {
                chart.ChartId = chartId.ToString();
            }

            // 游戏模式
            if (int.TryParse(meta["mode"]?.ToString(), out int mode))
            {
                chart.GameMode = mode;
            }

            // 修改时间（Unix时间戳）
            if (long.TryParse(meta["time"]?.ToString(), out long modifyTime))
            {
                chart.LastModifiedTime = modifyTime;
            }

            // 解析 song 子对象
            var song = meta["song"] as JObject;
            if (song != null)
            {
                chart.ChartName = song["title"]?.ToString() ?? "未知谱面";
                chart.Artist = song["artist"]?.ToString() ?? "未知艺术家";

                // 歌曲ID
                if (int.TryParse(song["id"]?.ToString(), out int songId))
                {
                    chart.SongId = songId;
                }

                // 原文信息
                chart.OriginalTitle = song["titleorg"]?.ToString();
                chart.OriginalArtist = song["artistorg"]?.ToString();
            }

            // 解析 mode_ext 子对象
            var modeExt = meta["mode_ext"] as JObject;
            if (modeExt != null)
            {
                // 轨道数（4K, 6K, 8K等）
                if (int.TryParse(modeExt["column"]?.ToString(), out int columns))
                {
                    chart.ColumnCount = columns;
                }

                // 起始小节
                if (int.TryParse(modeExt["bar_begin"]?.ToString(), out int barBegin))
                {
                    chart.BarBegin = barBegin;
                }
            }

            logger.Log(
                $"Meta信息: {chart.OriginalTitle} - {chart.OriginalArtist} - 难度: {chart.Version}"
            );
        }

        /// <summary>
        /// 解析 time 部分信息 - BPM变化
        /// </summary>
        private void ExtractBPMInfo(Chart chart, JArray timeArray, AsyncLogger logger)
        {
            var bpmChanges = new List<BPMChange>();

            foreach (var timeItem in timeArray)
            {
                if (timeItem is JObject timeObj)
                {
                    var beat = timeObj["beat"] as JArray;
                    var bpmToken = timeObj["bpm"];

                    if (beat != null && beat.Count >= 3 && bpmToken != null)
                    {
                        try
                        {
                            var bpmChange = new BPMChange
                            {
                                Measure = beat[0].Value<int>(),
                                Beat = beat[1].Value<int>(),
                                Division = beat[2].Value<int>(),
                                BPM = bpmToken.Value<double>(),
                            };

                            bpmChanges.Add(bpmChange);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning($"解析BPM变化数据失败: {ex.Message}");
                        }
                    }
                }
            }

            chart.BPMChanges = bpmChanges;

            // 设置基准BPM（取第一个BPM作为基准）
            if (bpmChanges.Count > 0)
            {
                chart.BPM = bpmChanges[0].BPM;
                logger.Log($"找到 {bpmChanges.Count} 个BPM变化点，基准BPM: {chart.BPM}");
            }
        }

        /// <summary>
        /// 解析 note 部分信息 - 音符和音频
        /// </summary>
        private void ExtractNoteInfo(Chart chart, JArray noteArray, AsyncLogger logger)
        {
            var notes = new List<Note>();
            string audioFile = null;
            int audioOffset = 0;

            foreach (var noteItem in noteArray)
            {
                if (noteItem is JObject noteObj)
                {
                    // 检查是否是音频信息（type=1）
                    var typeToken = noteObj["type"];
                    if (typeToken != null && typeToken.Value<int>() == 1)
                    {
                        audioFile = noteObj["sound"]?.ToString();
                        if (int.TryParse(noteObj["offset"]?.ToString(), out int offset))
                        {
                            audioOffset = offset;
                        }
                        if (int.TryParse(noteObj["vol"]?.ToString(), out int volume))
                        {
                            chart.AudioVolume = volume;
                        }
                        continue;
                    }

                    // 解析普通音符
                    var beat = noteObj["beat"] as JArray;
                    var columnToken = noteObj["column"];
                    var endBeat = noteObj["endbeat"] as JArray;
                    var sound = noteObj["sound"]?.ToString();

                    if (beat != null && beat.Count >= 3 && columnToken != null)
                    {
                        try
                        {
                            var note = new Note
                            {
                                Measure = beat[0].Value<int>(),
                                Beat = beat[1].Value<int>(),
                                Division = beat[2].Value<int>(),
                                TrackNum = columnToken.Value<int>(),
                                SoundFile = sound,
                            };

                            // 如果是Hold音符
                            if (endBeat != null && endBeat.Count >= 3)
                            {
                                note.IsHold = true;
                                note.EndMeasure = endBeat[0].Value<int>();
                                note.EndBeat = endBeat[1].Value<int>();
                                note.EndDivision = endBeat[2].Value<int>();
                            }

                            // 音量
                            if (int.TryParse(noteObj["vol"]?.ToString(), out int noteVolume))
                            {
                                note.Volume = noteVolume;
                            }

                            notes.Add(note);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning($"解析音符数据失败: {ex.Message}");
                        }
                    }
                }
            }

            chart.Notes = notes;
            chart.AudioFileName = audioFile;
            chart.AudioOffset = audioOffset;

            logger.Log($"找到 {notes.Count} 个音符，音频文件: {audioFile ?? "无"}");
        }

        /// <summary>
        /// 解析 extra 部分信息
        /// </summary>
        private void ExtractExtraInfo(Chart chart, JObject extra, AsyncLogger logger)
        {
            var test = extra["test"] as JObject;
            if (test != null)
            {
                // 分度值
                if (int.TryParse(test["divide"]?.ToString(), out int divide))
                {
                    chart.Divide = divide;
                }

                // 播放速度
                if (int.TryParse(test["speed"]?.ToString(), out int speed))
                {
                    chart.PlaySpeed = speed;
                }

                // 编辑模式等
                if (int.TryParse(test["edit_mode"]?.ToString(), out int editMode))
                {
                    chart.EditMode = editMode;
                }

                logger.Log($"Extra信息: 分度{chart.Divide} - 速度{chart.PlaySpeed}%");
            }
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 获取所有谱面
        /// </summary>
        public List<Chart> GetAllCharts(AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                var charts = ChartCollection.GetAllCharts().ToList();
                logger.Info($"获取所有谱面，总数: {charts.Count}");
                return charts;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 根据文件名查找谱面文件
        /// </summary>
        public ChartFile FindChartFile(string fileName, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                var chartFile = ChartCollection.ChartFiles.FirstOrDefault(f =>
                    f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                );

                if (chartFile != null)
                    logger.Info($"找到谱面文件: {fileName}");
                else
                    logger.Warning($"未找到谱面文件: {fileName}");

                return chartFile;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 获取指定谱面的音频文件数据
        /// </summary>
        public byte[] GetAudioData(Chart chart, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                var chartFile = ChartCollection.ChartFiles.FirstOrDefault(f =>
                    f.Charts.Contains(chart)
                );

                if (chartFile == null)
                {
                    logger.Error($"未找到包含谱面 {chart.ChartName} 的文件");
                    return null;
                }

                var audioFile = chartFile.AllFiles.FirstOrDefault(f =>
                    f.FileName.Equals(chart.AudioFileName, StringComparison.OrdinalIgnoreCase)
                );

                if (audioFile != null)
                {
                    logger.Info($"获取音频数据成功: {chart.AudioFileName}");
                    return audioFile.Data;
                }
                else
                {
                    logger.Error($"未找到音频文件: {chart.AudioFileName}");
                    return null;
                }
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// 获取指定谱面的封面文件数据
        /// </summary>
        public byte[] GetCoverData(Chart chart, AsyncLogger logger = null)
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(ChartParser), _config.EnableLogging);

            try
            {
                var chartFile = ChartCollection.ChartFiles.FirstOrDefault(f =>
                    f.Charts.Contains(chart)
                );

                if (chartFile == null)
                {
                    logger.Error($"未找到包含谱面 {chart.ChartName} 的文件");
                    return null;
                }

                var coverFile = chartFile.AllFiles.FirstOrDefault(f =>
                    f.FileName.Equals(chart.CoverFileName, StringComparison.OrdinalIgnoreCase)
                );

                if (coverFile != null)
                {
                    logger.Info($"获取封面数据成功: {chart.CoverFileName}");
                    return coverFile.Data;
                }
                else
                {
                    logger.Error($"未找到封面文件: {chart.CoverFileName}");
                    return null;
                }
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                LogManager.Info(
                    "释放 ChartParser 资源",
                    nameof(ChartParser),
                    _config.EnableLogging
                );
                _disposed = true;
            }
        }

        #endregion
    }
}
