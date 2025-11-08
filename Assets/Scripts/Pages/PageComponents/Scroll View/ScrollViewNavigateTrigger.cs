using EasingCore;
using FancyScrollView;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollViewNavigateTrigger : MonoBehaviour
{
    // 对Scroller的引用，需要在Inspector中赋值
    [SerializeField]
    private Scroller scroller;

    // 对滚动视图内容父物体的引用，通常是FancyScrollView的Content
    [SerializeField]
    private RectTransform contentPanel;

    private GameObject currentSelectedObject;

    void Update()
    {
        // 检查当前是否有被事件系统选中的UI元素
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            // 获取当前选中的对象
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            // 如果选中的对象发生了变化
            if (selected != currentSelectedObject)
            {
                currentSelectedObject = selected;

                // 检查选中的对象是否是滚动视图中的一个项目（按钮）
                // 这里假设你的滚动项按钮是contentPanel的直接子物体
                if (currentSelectedObject.transform.IsChildOf(contentPanel))
                {
                    // 获取该子物体在Content下的索引
                    int selectedIndex = currentSelectedObject.transform.GetSiblingIndex();

                    // 调用Scroller滚动到该索引位置
                    // 使用你期望的时长和缓动函数
                    scroller.ScrollTo(selectedIndex, 0.35f, Ease.OutCubic);
                }
            }
        }
        else
        {
            // 如果没有选中的对象，清空记录
            currentSelectedObject = null;
        }
    }
}
