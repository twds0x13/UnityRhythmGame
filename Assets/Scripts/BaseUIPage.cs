using System.Collections.Generic;
using PooledObjectNS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserInterfaceNS;

[RequireComponent(typeof(Canvas))]
public class BaseUIPage : MonoBehaviour
{
    protected bool AnimeWhenDestroy = true;

    protected Canvas SelfCanvas;

    private List<IPageControlled<MonoBehaviour>> ControlledObjects = new();

    public List<DevDisplayBehaviour> TextMesh = new();

    private void Awake()
    {
        SelfCanvas = GetComponent<Canvas>();
    }

    public virtual void OnOpenPage()
    {
        gameObject.SetActive(true);

        foreach (PooledObjectBehaviour Object in ControlledObjects)
        {
            Object.OnOpenPage();
        }

        foreach (DevDisplayBehaviour Object in TextMesh)
        {
            Object.OnOpenPage();
        }
    }

    public virtual void OnUpdatePage() { }

    public virtual void OnClosePage()
    {
        foreach (PooledObjectBehaviour Object in ControlledObjects)
        {
            Object.OnClosePage();
        }

        foreach (DevDisplayBehaviour Object in TextMesh)
        {
            Object.OnClosePage();
        }
    }

    public void RegisterObject(IPageControlled<MonoBehaviour> Controlled)
    {
        ControlledObjects.Add(Controlled);
    }

    public void UnregisterObject(IPageControlled<MonoBehaviour> Controlled)
    {
        ControlledObjects.Remove(Controlled);
    }
}
