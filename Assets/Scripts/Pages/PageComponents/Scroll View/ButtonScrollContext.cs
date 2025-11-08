using FancyScrollView;
using UnityEngine;

public class ButtonScrollContext : FancyScrollRectContext
{
    public int SelectedIndex { get; set; } = -1;

    public System.Action<int> OnButtonClicked { get; set; } // °´Å¥µã»÷´«µÝ ID
}
