using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

// using static ChartReader;

namespace Parser
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

        #region UniTask 音频转换方法

        /// <summary>
        /// 将音频数据转换为 AudioClip
        /// </summary>
        public async UniTask<AudioClip> ConvertToAudioClipAsync(
            byte[] audioData,
            string audioFileName,
            string clipName = "ChartAudio",
            CancellationToken cancellationToken = default
        )
        {
            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError("音频数据为空");
                return null;
            }

            try
            {
                // 根据文件扩展名确定音频格式
                AudioType audioType = GetAudioTypeFromFileName(audioFileName);

                if (audioType == AudioType.UNKNOWN)
                {
                    Debug.LogError($"不支持的音频格式: {audioFileName}");
                    return null;
                }

                // 方法1: 使用 UnityWebRequestMultimedia (推荐)
                return await LoadAudioWithUnityWebRequest(
                    audioData,
                    audioType,
                    clipName,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                Debug.Log("音频加载被取消");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"转换音频失败: {ex.Message}");
                // 尝试备用方法
                return await LoadAudioWithMemoryStream(
                    audioData,
                    audioFileName,
                    clipName,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// 直接从谱面获取 AudioClip (不使用 WWW)
        /// </summary>
        public async UniTask<AudioClip> GetAudioClipAsync(
            Chart chart,
            string clipName = null,
            CancellationToken cancellationToken = default
        )
        {
            if (chart == null)
            {
                LogManager.Error("谱面为空");
                return null;
            }

            // 异步获取音频数据
            byte[] audioData = await UniTask.RunOnThreadPool(
                () => GetAudioData(chart),
                cancellationToken: cancellationToken
            );

            if (audioData == null)
            {
                LogManager.Error($"无法获取谱面 {chart.ChartName} 的音频数据");
                return null;
            }

            return await ConvertToAudioClipAsync(
                audioData,
                chart.AudioFileName,
                clipName ?? chart.AudioFileName,
                cancellationToken
            );
        }

        /// <summary>
        /// 批量获取多个谱面的 AudioClip
        /// </summary>
        public async UniTask<Dictionary<Chart, AudioClip>> GetAudioClipsAsync(
            IEnumerable<Chart> charts,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var chartList = charts.ToList();
            var results = new Dictionary<Chart, AudioClip>();

            for (int i = 0; i < chartList.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var chart = chartList[i];
                try
                {
                    var audioClip = await GetAudioClipAsync(
                        chart,
                        $"{chart.ChartName}_Audio",
                        cancellationToken
                    );
                    if (audioClip != null)
                    {
                        results[chart] = audioClip;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载谱面 {chart.ChartName} 音频失败: {ex.Message}");
                }

                // 更新进度
                progress?.Report((float)(i + 1) / chartList.Count);

                // 每加载一个后等待一帧，避免卡顿
                await UniTask.Yield();
            }

            return results;
        }

        /// <summary>
        /// 预加载所有谱面的音频（带进度回调）
        /// </summary>
        public async UniTask PreloadAllAudioAsync(
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var allCharts = GetAllCharts();
            await GetAudioClipsAsync(allCharts, progress, cancellationToken);
        }

        #endregion

        #region 音频加载核心方法（不使用 WWW）

        /// <summary>
        /// 使用 UnityWebRequestMultimedia 加载音频（推荐方法）
        /// </summary>
        private async UniTask<AudioClip> LoadAudioWithUnityWebRequest(
            byte[] audioData,
            AudioType audioType,
            string clipName,
            CancellationToken cancellationToken
        )
        {
            // 创建临时文件路径
            string tempPath = Path.Combine(
                Application.temporaryCachePath,
                $"temp_audio_{Guid.NewGuid()}{GetAudioExtension(audioType)}"
            );

            try
            {
                // 写入临时文件
                await File.WriteAllBytesAsync(tempPath, audioData, cancellationToken);

                // 使用 UnityWebRequestMultimedia 加载音频
                using var www = UnityWebRequestMultimedia.GetAudioClip(
                    $"file://{tempPath}",
                    audioType
                );

                // 发送请求并等待完成
                await www.SendWebRequest().WithCancellation(cancellationToken);

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        clip.name = clipName;
                        return clip;
                    }
                    else
                    {
                        Debug.LogError("从 UnityWebRequest 获取 AudioClip 失败");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"加载音频失败: {www.error}");
                    return null;
                }
            }
            finally
            {
                // 清理临时文件
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"清理临时文件失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 使用 MemoryStream 和 NAudio 备用方案（如果需要更复杂的音频处理）
        /// </summary>
        private async UniTask<AudioClip> LoadAudioWithMemoryStream(
            byte[] audioData,
            string audioFileName,
            string clipName,
            CancellationToken cancellationToken
        )
        {
            // 这个方法作为备用方案，使用 System.IO 和可能的第三方库
            // 对于简单的 WAV 文件，可以手动解析

            try
            {
                // 检查文件类型
                if (audioFileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    return await ParseWavFile(audioData, clipName, cancellationToken);
                }
                else
                {
                    Debug.LogError($"不支持直接解析的音频格式: {audioFileName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"备用音频加载方法失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析 WAV 文件数据（简单的 WAV 解析器）
        /// </summary>
        private async UniTask<AudioClip> ParseWavFile(
            byte[] wavData,
            string clipName,
            CancellationToken cancellationToken
        )
        {
            return await UniTask.RunOnThreadPool(
                () =>
                {
                    try
                    {
                        using var memoryStream = new MemoryStream(wavData);
                        using var binaryReader = new BinaryReader(memoryStream);

                        // 读取 RIFF 头
                        string riff = new string(binaryReader.ReadChars(4));
                        if (riff != "RIFF")
                            throw new Exception("无效的 WAV 文件");

                        binaryReader.ReadInt32(); // 文件大小

                        string wave = new string(binaryReader.ReadChars(4));
                        if (wave != "WAVE")
                            throw new Exception("无效的 WAV 文件");

                        // 查找 fmt 块
                        while (memoryStream.Position < memoryStream.Length)
                        {
                            string chunkId = new string(binaryReader.ReadChars(4));
                            int chunkSize = binaryReader.ReadInt32();

                            if (chunkId == "fmt ")
                            {
                                // 读取音频格式信息
                                int audioFormat = binaryReader.ReadInt16();
                                int numChannels = binaryReader.ReadInt16();
                                int sampleRate = binaryReader.ReadInt32();
                                int byteRate = binaryReader.ReadInt32();
                                int blockAlign = binaryReader.ReadInt16();
                                int bitsPerSample = binaryReader.ReadInt16();

                                // 跳过剩余数据
                                if (chunkSize > 16)
                                    binaryReader.ReadBytes(chunkSize - 16);

                                // 查找 data 块
                                while (memoryStream.Position < memoryStream.Length)
                                {
                                    string dataChunkId = new string(binaryReader.ReadChars(4));
                                    int dataChunkSize = binaryReader.ReadInt32();

                                    if (dataChunkId == "data")
                                    {
                                        // 读取音频数据
                                        byte[] audioBytes = binaryReader.ReadBytes(dataChunkSize);

                                        // 计算样本数
                                        int samples =
                                            audioBytes.Length / (bitsPerSample / 8) / numChannels;

                                        // 创建 AudioClip
                                        AudioClip audioClip = AudioClip.Create(
                                            clipName,
                                            samples,
                                            numChannels,
                                            sampleRate,
                                            false
                                        );

                                        // 设置音频数据
                                        float[] audioData = ConvertByteToFloat(
                                            audioBytes,
                                            bitsPerSample
                                        );
                                        audioClip.SetData(audioData, 0);

                                        return audioClip;
                                    }
                                    else
                                    {
                                        // 跳过未知块
                                        binaryReader.ReadBytes(dataChunkSize);
                                    }
                                }
                            }
                            else
                            {
                                // 跳过未知块
                                binaryReader.ReadBytes(chunkSize);
                            }
                        }

                        throw new Exception("未找到音频数据");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"WAV 文件解析失败: {ex.Message}");
                        return null;
                    }
                },
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// 将字节数组转换为浮点数组
        /// </summary>
        private float[] ConvertByteToFloat(byte[] bytes, int bitsPerSample)
        {
            if (bitsPerSample == 16)
            {
                // 16-bit PCM
                int sampleCount = bytes.Length / 2;
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(bytes, i * 2);
                    samples[i] = sample / 32768f;
                }

                return samples;
            }
            else if (bitsPerSample == 8)
            {
                // 8-bit PCM
                float[] samples = new float[bytes.Length];

                for (int i = 0; i < bytes.Length; i++)
                {
                    samples[i] = (bytes[i] - 128) / 128f;
                }

                return samples;
            }
            else
            {
                throw new Exception($"不支持的位深度: {bitsPerSample}");
            }
        }

        #endregion

        #region 音频辅助方法

        /// <summary>
        /// 根据文件名获取 AudioType
        /// </summary>
        private AudioType GetAudioTypeFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return AudioType.UNKNOWN;

            string extension = Path.GetExtension(fileName).ToLower();

            switch (extension)
            {
                case ".mp3":
                    return AudioType.MPEG;
                case ".wav":
                    return AudioType.WAV;
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".aiff":
                case ".aif":
                    return AudioType.AIFF;
                default:
                    Debug.LogWarning($"未知的音频格式: {extension}，尝试自动检测");
                    return AudioType.UNKNOWN;
            }
        }

        /// <summary>
        /// 获取音频文件扩展名
        /// </summary>
        private string GetAudioExtension(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.MPEG:
                    return ".mp3";
                case AudioType.WAV:
                    return ".wav";
                case AudioType.OGGVORBIS:
                    return ".ogg";
                case AudioType.AIFF:
                    return ".aiff";
                default:
                    return ".audio";
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
