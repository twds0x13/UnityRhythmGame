using System.Collections.Generic;
using PageNS;
using UnityEngine;
using UnityEngine.UI;

public class StoryPage : BaseUIPage
{
    [Ext.ReadOnly]
    public Image CurrentBackGround;

    [Ext.ReadOnlyInGame]
    public Sprite[] StoryBackGrounds;

    public override void OnAwake() // 已解除作为剧情编辑器的任务，只用来展示剧情
    {
        SetName(nameof(StoryPage));

        PageOpenAnimeDuration = 0.5f;

        PageCloseAnimeDuration = 0.5f;

        base.OnAwake();

        // 注意 这里代表的是 StoryBackGround 这个游戏物体 作为被控制的 Image 物体

        CurrentBackGround = FindDisplayImage("StoryBackGround");

        CurrentBackGround.sprite = StoryBackGrounds[0];

        // LogManager.Info(StoryBackGround.name, nameof(StoryPage));
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();
    }

    public override void OnClosePage()
    {
        base.OnClosePage();
    }

    public void ReturnToMain()
    {
        Manager.SwitchToPage(nameof(MainPage));
    }

    public void ReturnToGame()
    {
        Manager.SwitchToPage(nameof(GamePage));
    }

    public void NextBackGround(int num)
    {
        if (StoryBackGrounds is null || StoryBackGrounds.Length == 0)
        {
            LogManager.Warning("StoryBackGrounds is null or empty", nameof(StoryPage));
            return;
        }
        if (num < 0 || num >= StoryBackGrounds.Length)
        {
            LogManager.Warning("num out of range", nameof(StoryPage));
            return;
        }
        if (StoryBackGrounds[num] is not null)
        {
            CurrentBackGround.sprite = StoryBackGrounds[num];
        }
    }
}
