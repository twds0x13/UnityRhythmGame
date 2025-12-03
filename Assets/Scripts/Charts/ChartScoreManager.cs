using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Singleton;
using UnityEngine;

public enum ScoreRank
{
    NotPlayed,
    F,
    D,
    C,
    B,
    A,
    S,
    S_plus,
    SS,
    SSS,
    SSS_plus,
    LOVE,
}

public static class ScoreRankScores
{
    public const float F = 0.0f;
    public const float D = 80000.0f;
    public const float C = 85000.0f;
    public const float B = 90000.0f;
    public const float A = 95000.0f;
    public const float S = 97000.0f;
    public const float S_plus = 98000.0f;
    public const float SS = 99000.0f;
    public const float SSS = 100000.0f;
    public const float SSS_plus = 105000.0f;
    public const float LOVE = 110000.0f;
    public const float MaxChartScore = LOVE;
}

public enum ClearState
{
    NotPlayed = 0, // 未游玩
    Failed = 1, // 失败
    Cleared = 2, // 通过
    FullCombo = 3, // 全连
    AllPerfect = 4, // 全完美
}

public class ChartScoreData
{
    public string ChartId { get; set; } // 关联谱面ID
    public ClearState ClearState { get; set; } // 通关状态

    public ScoreRank ScoreRankState { get; set; } // 评分等级状态
    public float CompletionRate { get; set; } // 完成率 0-100%
    public int HighScore { get; set; } // 最高分
    public int MaxCombo { get; set; } // 最大连击
    public float Accuracy { get; set; } // 准确率（仅用于这次显示）
    public int PlayCount { get; set; } // 游玩次数
    public DateTime LastPlayed { get; set; } // 最后游玩时间

    public void UpdateWithNewPlay(ChartScoreData newPlayData)
    {
        if (newPlayData == null)
            return;

        PlayCount++;
        LastPlayed = DateTime.Now;

        // 使用 Mathf.Max 或 Math.Max 确保取最大值
        HighScore = Math.Max(HighScore, newPlayData.HighScore);
        MaxCombo = Math.Max(MaxCombo, newPlayData.MaxCombo);
        CompletionRate = Math.Max(CompletionRate, newPlayData.CompletionRate);

        // 更新准确率（记录最新）
        Accuracy = newPlayData.Accuracy;

        // 状态更新
        ClearState = (ClearState)Math.Max((int)ClearState, (int)newPlayData.ClearState);

        ScoreRankState = (ScoreRank)Math.Max((int)ScoreRankState, (int)newPlayData.ScoreRankState);
    }

    // 原有的 CreateNew 方法保持不变
    public static ChartScoreData CreateNew(string chartId)
    {
        return new ChartScoreData
        {
            ChartId = chartId,
            ClearState = ClearState.NotPlayed,
            ScoreRankState = ScoreRank.NotPlayed,
            PlayCount = 0,
            HighScore = 0,
            MaxCombo = 0,
            Accuracy = 0.0f,
            CompletionRate = 0.0f,
            LastPlayed = DateTime.MinValue,
        };
    }
}

public static class ScoreRankCalculator
{
    public static float ClassifyScore(float score, float maxScore) =>
        ScoreRankScores.MaxChartScore * (score / maxScore);

    /// <summary>
    /// 输入已格式化的分数，返回对应的评分等级
    /// </summary>
    /// <param name="score"></param>
    /// <returns></returns>
    public static ScoreRank CalculateScoreRank(float score) =>
        score switch
        {
            >= ScoreRankScores.LOVE => ScoreRank.LOVE,
            >= ScoreRankScores.SSS_plus => ScoreRank.SSS_plus,
            >= ScoreRankScores.SSS => ScoreRank.SSS,
            >= ScoreRankScores.SS => ScoreRank.SS,
            >= ScoreRankScores.S_plus => ScoreRank.S_plus,
            >= ScoreRankScores.S => ScoreRank.S,
            >= ScoreRankScores.A => ScoreRank.A,
            >= ScoreRankScores.B => ScoreRank.B,
            >= ScoreRankScores.C => ScoreRank.C,
            >= ScoreRankScores.D => ScoreRank.D,
            _ => ScoreRank.F,
        };
}

public interface IChartScoreManager
{
    void InitChartScores(bool overrideExistingScore);

    void SaveChartScore(ChartScoreData data);

    void SaveChartScores(List<ChartScoreData> scores);

    void DeleteChartScore(ChartScoreData data);
    void DeleteAllScores();
    List<ChartScoreData> LoadChartScores();
    void RefreshChartScores();
}

public class ChartScoreManager : Singleton<ChartScoreManager>, IChartScoreManager
{
    private string SavePath;
    private List<ChartScoreData> ChartScores;
    private const string SAVE_FILE_NAME = "chart_scores.json";

    protected override void SingletonAwake()
    {
        SavePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        RefreshChartScores();

        // 初始化日志
        LogManager.Log(
            $"ChartScoreManager 初始化完成，保存路径: {SavePath}",
            nameof(ChartScoreManager),
            true
        );
    }

    public void SaveChartScore(ChartScoreData data)
    {
        if (data == null || string.IsNullOrEmpty(data.ChartId))
        {
            LogManager.Warning(
                "ChartScoreData 或 ChartId 为空，无法保存",
                nameof(ChartScoreManager),
                true
            );
            return;
        }

        // 检查是否已存在相同ID的记录
        var existingIndex = ChartScores.FindIndex(score => score.ChartId == data.ChartId);

        if (existingIndex >= 0)
        {
            // 更新现有记录 - 调用新的 UpdateWithNewPlay 方法
            var existingData = ChartScores[existingIndex];
            existingData.UpdateWithNewPlay(data);

            LogManager.Log(
                $"更新传入的分数记录: ID={data.ChartId}, HighScore={data.HighScore}, PlayCount={data.PlayCount},MaxCombo={data.MaxCombo}",
                nameof(ChartScoreManager),
                true
            );

            LogManager.Log(
                $"已更新谱面ID为 {data.ChartId} 的分数记录",
                nameof(ChartScoreManager),
                true
            );
        }
        else
        {
            // 新增记录 - 使用传入的数据，不覆盖 ClearState
            if (data.PlayCount == 0)
            {
                data.PlayCount = 1;
            }

            // 确保最后游玩时间正确
            if (data.LastPlayed == DateTime.MinValue)
            {
                data.LastPlayed = DateTime.Now;
            }

            ChartScores.Add(data);

            LogManager.Log(
                $"已添加新谱面ID为 {data.ChartId} 的分数记录",
                nameof(ChartScoreManager),
                true
            );
        }

        // 保存到文件
        SaveToFile();
    }

    public void DeleteChartScore(ChartScoreData data)
    {
        if (data == null || string.IsNullOrEmpty(data.ChartId))
        {
            LogManager.Warning(
                "ChartScoreData 或 ChartId 为空，无法删除",
                nameof(ChartScoreManager),
                true
            );
            return;
        }

        // 只根据ID删除，忽略其他字段
        int removedCount = ChartScores.RemoveAll(score => score.ChartId == data.ChartId);

        if (removedCount > 0)
        {
            SaveToFile();
            LogManager.Log(
                $"已删除谱面ID为 {data.ChartId} 的分数记录",
                nameof(ChartScoreManager),
                true
            );
        }
        else
        {
            LogManager.Log(
                $"未找到谱面ID为 {data.ChartId} 的分数记录",
                nameof(ChartScoreManager),
                true
            );
        }
    }

    public List<ChartScoreData> LoadChartScores()
    {
        if (!File.Exists(SavePath))
        {
            LogManager.Log($"分数文件不存在，创建新的空列表", nameof(ChartScoreManager), true);
            return new List<ChartScoreData>();
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            if (string.IsNullOrEmpty(json))
            {
                LogManager.Log($"分数文件为空，返回空列表", nameof(ChartScoreManager), true);
                return new List<ChartScoreData>();
            }

            var scores = JsonConvert.DeserializeObject<List<ChartScoreData>>(json);

            if (scores == null)
            {
                LogManager.Log(
                    $"分数文件反序列化失败，返回空列表",
                    nameof(ChartScoreManager),
                    true
                );
                return new List<ChartScoreData>();
            }

            LogManager.Log($"成功加载 {scores.Count} 条分数记录", nameof(ChartScoreManager), true);
            return scores;
        }
        catch (Exception e)
        {
            LogManager.Error($"加载分数文件失败: {e.Message}", nameof(ChartScoreManager), true);
            return new List<ChartScoreData>();
        }
    }

    public void RefreshChartScores()
    {
        ChartScores = LoadChartScores();
    }

    // 私有方法：将数据保存到文件
    private void SaveToFile()
    {
        try
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(SavePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);

                LogManager.Log($"创建目录: {directory}", nameof(ChartScoreManager), true);
            }

            ChartScores = ChartScores.OrderBy(score => int.Parse(score.ChartId)).ToList();

            // 序列化并保存
            string json = JsonConvert.SerializeObject(ChartScores, Formatting.Indented);

            File.WriteAllText(SavePath, json);

            LogManager.Info(
                $"分数数据已保存到: {SavePath}，共 {ChartScores.Count} 条记录",
                nameof(ChartScoreManager),
                true
            );
        }
        catch (Exception e)
        {
            LogManager.Error($"保存分数文件失败: {e.Message}", nameof(ChartScoreManager), true);
        }
    }

    public void InitChartScores(bool overrideExistingScore = false)
    {
        if (overrideExistingScore)
        {
            ChartScores = new List<ChartScoreData>();

            foreach (var chart in ChartManager.Inst.AllCharts)
            {
                var newScore = ChartScoreData.CreateNew(chart.ChartId);
                ChartScores.Add(newScore);
            }

            SaveToFile();

            LogManager.Warning("分数数据已被重置", nameof(ChartScoreManager), true);
        }
        else
        {
            // 检查文件是否存在
            bool fileExists = File.Exists(SavePath);

            if (!fileExists || ChartScores.Count == 0)
            {
                // 文件不存在或文件为空，创建新的分数记录
                LogManager.Log(
                    "检测到分数文件不存在或为空，创建新的分数记录",
                    nameof(ChartScoreManager),
                    true
                );

                ChartScores = new List<ChartScoreData>();

                // 使用 ChartManager 中的谱面列表创建新的分数记录

                if (ChartManager.Inst.AllCharts != null && ChartManager.Inst.AllCharts.Count > 0)
                {
                    foreach (var chart in ChartManager.Inst.AllCharts)
                    {
                        var newScore = ChartScoreData.CreateNew(chart.ChartId);
                        ChartScores.Add(newScore);
                    }

                    SaveToFile();
                    LogManager.Log(
                        $"已创建 {ChartManager.Inst.AllCharts.Count} 个谱面的默认分数记录",
                        nameof(ChartScoreManager),
                        true
                    );
                }
                else
                {
                    LogManager.Warning(
                        "ChartManager.AllCharts 为空，无法创建默认分数记录",
                        nameof(ChartScoreManager),
                        true
                    );
                }
            }
            else
            {
                // 文件存在且有数据，正常刷新
                RefreshChartScores();

                // 可选：检查是否需要为新增的谱面添加默认记录
                CheckAndAddMissingChartScores();

                LogManager.Log("分数数据已加载", nameof(ChartScoreManager), true);
            }
        }
    }

    // 新增方法：检查并添加缺失的谱面分数记录
    private void CheckAndAddMissingChartScores()
    {
        if (ChartManager.Inst.AllCharts == null || ChartManager.Inst.AllCharts.Count == 0)
        {
            return;
        }

        int addedCount = 0;

        foreach (var chart in ChartManager.Inst.AllCharts)
        {
            // 检查是否已存在该谱面的分数记录
            var existingScore = ChartScores.FirstOrDefault(score => score.ChartId == chart.ChartId);

            if (existingScore == null)
            {
                // 谱面不存在，添加默认记录
                var newScore = ChartScoreData.CreateNew(chart.ChartId);
                ChartScores.Add(newScore);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            SaveToFile();
            LogManager.Log(
                $"已添加 {addedCount} 个新增谱面的默认分数记录",
                nameof(ChartScoreManager),
                true
            );
        }
    }

    public void DeleteAllScores()
    {
        ChartScores.Clear();
        SaveToFile();
        LogManager.Warning("所有分数数据已清空", nameof(ChartScoreManager), true);
    }

    public void SaveChartScores(List<ChartScoreData> scores)
    {
        if (scores == null || scores.Count == 0)
        {
            LogManager.Warning("批量保存的分数列表为空", nameof(ChartScoreManager), true);
            return;
        }

        LogManager.Log($"开始批量保存 {scores.Count} 条分数记录", nameof(ChartScoreManager), true);

        foreach (var score in scores)
        {
            SaveChartScore(score);
        }

        LogManager.Info(
            $"批量保存完成，共处理 {scores.Count} 条记录",
            nameof(ChartScoreManager),
            true
        );
    }
}
