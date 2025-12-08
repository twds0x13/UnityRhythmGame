using System;
using System.Collections.Generic;
using EasingCore;
using FancyScrollView;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SongSelectScrollView : FancyScrollView<ButtonScrollData, ButtonScrollContext>
{
    [SerializeField]
    private List<Image> selectedSongCover;

    [SerializeField]
    private List<Image> gameSongCover;

    [SerializeField]
    private TextMeshProUGUI selectedSongName;

    [SerializeField]
    private Scroller scroller;

    [SerializeField]
    private List<ButtonScrollData> cellData = new();

    [SerializeField]
    private GameObject cellPrefab;

    public Action<int> OnButtonAction;

    protected override GameObject CellPrefab => cellPrefab;

    protected override void Initialize()
    {
        base.Initialize();

        Context.OnButtonClicked = OnButtonClicked;

        scroller.OnValueChanged(UpdatePosition);

        scroller.OnSelectionChanged(UpdateSelection);
    }

    public void UpdateData(List<ButtonScrollData> newData)
    {
        cellData = newData ?? new();

        scroller.SetTotalCount(cellData.Count);

        UpdateContents(cellData);
    }

    public void UpdateSelection(int index)
    {
        if (Context.SelectedIndex == index)
        {
            return;
        }

        Context.SelectedIndex = index;

        selectedSongName.text = cellData[Context.SelectedIndex].SongTitle;

        foreach (var Cover in selectedSongCover)
        {
            Cover.sprite = cellData[Context.SelectedIndex].Cover;
        }

        foreach (var Cover in gameSongCover)
        {
            Cover.sprite = cellData[Context.SelectedIndex].Cover;
        }

        OnButtonClicked(index);

        LogManager.Log(
            $"Selected Index: {Context.SelectedIndex}",
            nameof(SongSelectScrollView),
            false
        );
    }

    protected override void UpdatePosition(float position)
    {
        base.UpdatePosition(position);
    }

    private void Scroll(int index)
    {
        scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
    }

    /// <summary>
    /// 添加按钮点击回调
    /// </summary>
    public void OnButtonClicked(int index)
    {
        // 根据 ID 找到对应的数据项
        var item = cellData.Find(i => i.Index == index);

        if (item != null)
        {
            HandleButtonAction(index);
        }
        else
        {
            LogManager.Warning($"未找到 ID 为 {index} 的数据项", nameof(SongSelectScrollView));
        }

        Scroll(index);
    }

    private void HandleButtonAction(int index)
    {
        OnButtonAction?.Invoke(index);
    }
}
