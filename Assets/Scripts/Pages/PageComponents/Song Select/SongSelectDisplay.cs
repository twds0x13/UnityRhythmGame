using System.Collections.Generic;
using PageNS;
using UIManagerNS;
using UnityEngine;
using UnityEngine.UI;
using Comp = TextManagerNS.PageComponentManager;

[RequireComponent(typeof(Selectable))]
public class SongSelectDisplay : MonoBehaviour, IPageComponent
{
    [Header("Display Controller")]
    [SerializeField]
    List<Comp.DynamicNum> DynamicEnumList;

    public void SetParentPage(BaseUIPage Parent) { }

    public void SetResizeDetector(ResizeDetector Detector) { }

    public void OnAwake()
    {
        gameObject.SetActive(false);
    }

    public void OnOpenPage()
    {
        gameObject.SetActive(true);
    }

    public void OnClosePage()
    {
        // gameObject.SetActive(false);
    }

    private void Update() { }
}
