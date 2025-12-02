using System;
using System.Collections;
using System.Collections.Generic;
using Singleton;
using UnityEngine;

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
    public ClearState ClearState { get; set; }
    public float CompletionRate { get; set; } // 完成率 0-100%
    public int HighScore { get; set; } // 最高分
    public int MaxCombo { get; set; } // 最大连击
    public float Accuracy { get; set; } // 准确率
    public int PlayCount { get; set; } // 游玩次数
    public DateTime LastPlayed { get; set; } // 最后游玩时间
    // 其他游戏特定数据...
}

public interface IChartScoreManager { }

public class ChartScoreManager : Singleton<ChartScoreManager>, IChartScoreManager
{
    protected override void SingletonAwake() { }
}
