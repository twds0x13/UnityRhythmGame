using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioNS;
using Cysharp.Threading.Tasks;
using Parser;
using Singleton;
using UnityEngine;
using UnityEngine.Events;

public class ChartManager : Singleton<ChartManager>
{
    public ChartParser Parser;

    public ChartCollection Collection;

    public List<Chart> AllCharts;

    public Chart SelectedChart;

    public UnityEvent OnInitialized;

    protected override void SingletonAwake()
    {
        var path = Path.Combine(Application.streamingAssetsPath);

        var config = new ChartParser.ParserConfig { EnableLogging = false };

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
}
