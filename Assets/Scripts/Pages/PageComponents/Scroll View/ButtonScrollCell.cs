using System;
using FancyScrollView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonScrollCell : FancyScrollRectCell<ButtonScrollData, ButtonScrollContext>
{
    [SerializeField]
    private TextMeshProUGUI songTitle;

    [SerializeField]
    private TextMeshProUGUI difficulty;

    [SerializeField]
    private Image songCover;

    [SerializeField]
    private Button cellButton; // 覆盖整个单元格

    [SerializeField]
    private Image backGround; // 选中高亮效果

    private ButtonScrollData currentData;

    // 初始化方法
    void Start()
    {
        // 注册整个单元格的点击事件
        if (cellButton != null)
        {
            cellButton.onClick.AddListener(OnButtonClicked);
        }
    }

    // 更新单元格内容
    public override void UpdateContent(ButtonScrollData itemData)
    {
        currentData = itemData;

        if (songTitle != null)
            songTitle.text = itemData.SongTitle;

        if (difficulty != null)
            difficulty.text = $"Lv. {itemData.Difficulty}";

        if (songCover != null)
            songCover.sprite = itemData.Cover;
    }

    // 更新单元格位置（用于动画效果）
    public override void UpdatePosition(float position)
    {
        transform.localPosition = new Vector3(0, -1.5f * (1080f * position - 540f), 0);

        var isSelected = Context != null && Context.SelectedIndex == Index;

        backGround.color = isSelected ? new Color(0.5f, 1f, 1f) : new Color(1f, 1f, 1f);
    }

    // 整个单元格点击事件
    private void OnButtonClicked()
    {
        Context?.OnButtonClicked?.Invoke(Index);
    }
}
