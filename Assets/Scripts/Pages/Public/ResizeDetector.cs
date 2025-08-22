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
            if (Mathf.Abs(Rect.rect.height - 1080f) < 1f) // ��һЩ�ض�����£�����Ϊ Free, 16:9 �� 16:10����Unity �Զ���������Ļ�߶Ⱥ� 1080f ���� Pixel Perfect ��Ϊɶ����
            {
                Debug.Log($"Resize Detected: {Rect.rect.width} x {Rect.rect.height}");

                ResizeEvent.Invoke();
            }
        }
    }
}
