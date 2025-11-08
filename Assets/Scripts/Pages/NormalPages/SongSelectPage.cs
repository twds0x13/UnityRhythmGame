using System.Collections.Generic;
using PageNS;
using Parser;
using UnityEngine;

public class SongSelectPage : BaseUIPage
{
    public SongSelectScrollView scrollView;

    private List<ButtonScrollData> cellData = new();

    private int counter = 0;

    public override void OnAwake()
    {
        SetName(nameof(SongSelectPage));

        PageOpenAnimeDuration = 0.5f;

        PageCloseAnimeDuration = 0.5f;

        scrollView.OnButtonAction = OnButtonAction;

        base.OnAwake();
    }

    public void InitSongSelect() // 由 UnityEvent 调用，在读取完所有谱面后立刻初始化
    {
        foreach (var chart in ChartManager.Inst.AllCharts)
        {
            LogManager.Log($"{chart.OriginalTitle}", nameof(SongSelectPage), false);

            AddOneItem(chart);
        }

        scrollView.UpdateData(cellData); // 那么这里就必须一次性刷新
    }

    public void AddOneItem(Chart data)
    {
        var newData = new ButtonScrollData
        {
            Index = counter++,

            Difficulty = Random.Range(1, 15),

            SongTitle = data.OriginalTitle,

            Cover = null,
        };

        cellData.Add(newData); // 单独添加新数据项暂不刷新
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();
    }

    public override void OnClosePage()
    {
        base.OnClosePage();
    }

    public void OnButtonAction(int id) => ChartManager.Inst.SelectChart(id);

    public void OnStartGame()
    {
        Manager.SwitchToPage(nameof(GamePage));
    }

    public void OnReturnToMain()
    {
        Manager.SwitchToPage(nameof(MainPage));
    }
}
