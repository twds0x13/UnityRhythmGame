using Singleton;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class ResizeDetector : Singleton<ResizeDetector>
{
    public UnityEvent ResizeEvent;

    public RectTransform Rect;

    public bool Lock = true;

    protected override void SingletonAwake()
    {
        ResizeEvent = new UnityEvent();
    }

    private void Update()
    {
        Lock = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!Lock)
        {
            if (Mathf.Abs(Rect.rect.height - 1080f) < 1f) // 在一些特定情况下（设置为 Free, 16:9 和 16:10），Unity 自动调整的屏幕高度和 1080f 并非 Pixel Perfect （为啥？）
            {
                Debug.Log($"Resize Detected: {Rect.rect.width} x {Rect.rect.height}");

                ResizeEvent.Invoke();
            }
        }
    }
}
