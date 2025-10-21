using System.Collections.Generic;
using NavigatorNS;
using PageNS;
using UIManagerNS;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Comp = TextManagerNS.PageComponentManager;

[RequireComponent(typeof(UINavigator)), RequireComponent(typeof(Selectable))]
public class SelectableDisplay : MonoBehaviour, IPageComponent
{
    [Header("Display Controller")]
    [SerializeField]
    List<Comp.DynamicNum> DynamicEnumList;

    [Ext.ReadOnlyInGame, SerializeField]
    UINavigator Navigator; // 负责控制 ParentRect ( 这个物体的 RectTransform )

    [Ext.ReadOnlyInGame, SerializeField]
    LocalizeStringEvent LocalizeStringEvent;

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

    public void RefreshText()
    {
        LocalizeStringEvent.RefreshString();
    }

    public void OnClosePage()
    {
        Navigator.Disappear();

        // gameObject.SetActive(false);
    }

    private void Update()
    {
        if (DynamicEnumList.Count > 0)
        {
            LocalizeStringEvent.StringReference.Arguments = Comp.Inst.GetDynamicNumsFromList(
                DynamicEnumList
            );

            RefreshText();
        }
    }
}
