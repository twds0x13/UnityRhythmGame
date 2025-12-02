using System.Collections.Generic;
using NavigatorNS;
using PageNS;
using UIManagerNS;
using UnityEngine;
using Comp = TextManagerNS.PageComponentManager;

[RequireComponent(typeof(UINavigator))]
public class ScrollDisplay : MonoBehaviour, IPageComponent
{
    [Header("Display Controller")]
    [SerializeField]
    List<Comp.DynamicNum> DynamicEnumList;

    [Ext.ReadOnlyInGame, SerializeField]
    UINavigator Navigator; // 负责控制 ParentRect ( 这个物体的 RectTransform )

    [Ext.ReadOnly, SerializeField]
    BaseUIPage ParentPage;

    public void SetParentPage(BaseUIPage Parent) // 在 OnAwake 之后由 ParentPage 调用一次
    {
        ParentPage = Parent;
        Navigator.Init(ParentPage);
    }

    public void SetResizeDetector(ResizeDetector Detector)
    {
        Navigator.SetResizeDetector(Detector);
    }

    public void OnAwake()
    {
        gameObject.SetActive(false);
    }

    public void OnOpenPage()
    {
        gameObject.SetActive(true);

        Navigator.Append();
    }

    public void RefreshText() { }

    public void OnClosePage()
    {
        Navigator.Disappear();

        // gameObject.SetActive(false);
    }

    private void Update()
    {
        if (DynamicEnumList.Count > 0)
        {
            RefreshText();
        }
    }
}
