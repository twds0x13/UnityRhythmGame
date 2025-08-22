using System;
using DG.Tweening;
using PageNS;
using TransformExtentionsNS;
using UnityEngine;

namespace NavigatorNS
{
    /// <summary>
    /// 管理 <see cref="BaseUIPage"/> 中组件的出现动画和结束动画，和界面缩放时的自动重定位
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UINavigator : MonoBehaviour
    {
        public enum AxisType
        {
            LeftUp,
            RightUp,
            LeftDown,
            RightDown,
        }

        public enum PivotType
        {
            Middle,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
        }

        public enum CanvasEdge
        {
            TopEdge,
            BottomEdge,
            LeftEdge,
            RightEdge,
        }

        public enum AppendTypeEnum
        {
            Slide,
            Pop,
        }

        public enum DisappearTypeEnum
        {
            Slide,
            Pop,
        }

        [Ext.ReadOnlyInGame, SerializeField]
        public AxisType NavigateAxis;

        [Ext.ReadOnlyInGame, SerializeField]
        public Vector3 AppendDestination;

        [Ext.ReadOnlyInGame, SerializeField]
        private PivotType SelfPivot;

        [SerializeField]
        private CanvasEdge AppendEdge;

        [SerializeField]
        private CanvasEdge DisappearEdge;

        [SerializeField]
        private AppendTypeEnum AppendType;

        [SerializeField]
        private DisappearTypeEnum DisappearType;

        [SerializeField]
        private Ease AppendEase;

        [SerializeField]
        private Ease DisappearEase;

        [Ext.ReadOnly, SerializeField]
        private BaseUIPage ParentPage;

        [Ext.ReadOnlyInGame, SerializeField]
        private RectTransform SelfRect;

        [Ext.ReadOnly, SerializeField]
        private ResizeDetector ResizeDetector;

        public Action ActionOnCompleteAppendAnime;

        public Action ActionOnCompleteDisappearAnime;

        private Tween AppendTween;

        private Tween DisappearTween;

        public UINavigator Init(BaseUIPage Page)
        {
            ParentPage = Page;
            return this;
        }

        public void SetResizeDetector(ResizeDetector Detector)
        {
            ResizeDetector = Detector;
            ResizeDetector.ResizeEvent.AddListener(OnResetScreenScale);
        }

        public void Append()
        {
            switch (AppendType)
            {
                case AppendTypeEnum.Slide:
                    SlideAppend(AppendEdge);
                    break;
            }
        }

        public void Disappear()
        {
            switch (DisappearType)
            {
                case DisappearTypeEnum.Slide:
                    SlideDisappear(DisappearEdge);
                    break;
            }
        }

        private void SlideAppend(CanvasEdge Edge)
        {
            Rect Rect = ResizeDetector.Rect.rect;

            GetAppendStartPoint(Edge);

            SetPivotByType(SelfPivot);

            AppendTween = transform
                .DOLocalMove(FormatAxis(AppendDestination), ParentPage.PageOpenAnimeDuration)
                .SetEase(AppendEase)
                .OnComplete(CompleteAppend);
        }

        private void SlideDisappear(CanvasEdge Edge)
        {
            DisappearTween = transform
                .DOLocalMove(GetDisappearPoint(Edge), ParentPage.PageCloseAnimeDuration)
                .SetEase(DisappearEase)
                .OnComplete(CompleteDisappear);
        }

        private void CompleteAppend()
        {
            ActionOnCompleteAppendAnime?.Invoke();
        }

        private void CompleteDisappear()
        {
            ActionOnCompleteDisappearAnime?.Invoke();
        }

        private Vector3 GetAppendStartPoint(CanvasEdge AppendEdge)
        {
            Rect Rect = ResizeDetector.Rect.rect;

            switch (AppendEdge)
            {
                case CanvasEdge.TopEdge:
                    transform.SetLocalPositionY(0.5f * Rect.height + SelfRect.rect.height);
                    transform.SetLocalPositionX(FormatAxis(AppendDestination).x);
                    break;

                case CanvasEdge.BottomEdge:
                    transform.SetLocalPositionY(-0.5f * Rect.height - SelfRect.rect.height);
                    transform.SetLocalPositionX(FormatAxis(AppendDestination).x);
                    break;

                case CanvasEdge.LeftEdge:
                    transform.SetLocalPositionX(-0.5f * Rect.width - SelfRect.rect.width);
                    transform.SetLocalPositionY(FormatAxis(AppendDestination).y);
                    break;

                case CanvasEdge.RightEdge:
                    transform.SetLocalPositionX(0.5f * Rect.width + SelfRect.rect.width);
                    transform.SetLocalPositionY(FormatAxis(AppendDestination).y);
                    break;
            }

            return transform.localPosition;
        }

        private Vector3 GetDisappearPoint(CanvasEdge Edge)
        {
            Rect Rect = ResizeDetector.Rect.rect;

            switch (Edge)
            {
                case CanvasEdge.LeftEdge:
                    return new Vector3(
                        -0.5f * Rect.width - SelfRect.rect.width,
                        transform.localPosition.y,
                        0f
                    );
                case CanvasEdge.RightEdge:
                    return new Vector3(
                        0.5f * Rect.width + SelfRect.rect.width,
                        transform.localPosition.y,
                        0f
                    );
                case CanvasEdge.TopEdge:
                    return new Vector3(
                        transform.localPosition.x,
                        0.5f * Rect.height + SelfRect.rect.height,
                        0f
                    );
                case CanvasEdge.BottomEdge:
                    return new Vector3(
                        transform.localPosition.x,
                        -0.5f * Rect.height - SelfRect.rect.height,
                        0f
                    );
            }

            throw new ArgumentOutOfRangeException();
        }

        public void SetPivotByType(PivotType Type) // 需要严密观察
        {
            switch (Type)
            {
                case PivotType.Middle:
                    SelfRect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case PivotType.Top:
                    SelfRect.pivot = new Vector2(0.5f, 1f);
                    break;
                case PivotType.Bottom:
                    SelfRect.pivot = new Vector2(0.5f, 0f);
                    break;
                case PivotType.Left:
                    SelfRect.pivot = new Vector2(0f, 0.5f);
                    break;
                case PivotType.Right:
                    SelfRect.pivot = new Vector2(1f, 0.5f);
                    break;
                case PivotType.TopLeft:
                    SelfRect.pivot = new Vector2(0f, 1f);
                    break;
                case PivotType.TopRight:
                    SelfRect.pivot = new Vector2(1f, 1f);
                    break;
                case PivotType.BottomLeft:
                    SelfRect.pivot = new Vector2(0f, 0f);
                    break;
                case PivotType.BottomRight:
                    SelfRect.pivot = new Vector2(1f, 0f);
                    break;
            }
        }

        public void OnResetScreenScale()
        {
            AppendTween.Kill();
            DisappearTween.Kill();
            Append();
        }

        private Vector3 FormatAxis(Vector3 AppendPosition)
        {
            Rect Rect = ResizeDetector.Rect.rect;

            switch (NavigateAxis)
            {
                case AxisType.LeftUp: // Canvas 左上角为坐标 0 点，右下角为（1，1）
                    return new Vector3(
                        -0.5f * Rect.width + AppendPosition.x * Rect.width,
                        0.5f * Rect.height - AppendPosition.y * Rect.height,
                        AppendPosition.z
                    );
                case AxisType.RightUp: // Canvas 右上角为坐标 0 点，左下角为（1，1）
                    return new Vector3(
                        0.5f * Rect.width - AppendPosition.x * Rect.width,
                        0.5f * Rect.height - AppendPosition.y * Rect.height,
                        AppendPosition.z
                    );
                case AxisType.LeftDown: // Canvas 左下角为坐标 0 点，右上角为（1，1）
                    return new Vector3(
                        -0.5f * Rect.width + AppendPosition.x * Rect.width,
                        -0.5f * Rect.height + AppendPosition.y * Rect.height,
                        AppendPosition.z
                    );
                case AxisType.RightDown: // Canvas 右下角为坐标 0 点，左上角为（1，1）
                    return new Vector3(
                        0.5f * Rect.width - AppendPosition.x * Rect.width,
                        -0.5f * Rect.height + AppendPosition.y * Rect.height,
                        AppendPosition.z
                    );
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
