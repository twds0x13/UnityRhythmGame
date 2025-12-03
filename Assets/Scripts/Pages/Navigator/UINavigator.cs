using System;
using DG.Tweening;
using PageNS;
using TransformExtentionsNS;
using UIManagerNS;
using UnityEngine;

namespace NavigatorNS
{
    /// <summary>
    /// 管理 <see cref="BaseUIPage"/> 中组件的出现动画和结束动画，和界面缩放时的自动重定位
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UINavigator : MonoBehaviour
    {
        public enum NavigateModeType
        {
            Percent,
            Axis,
        }

        public enum AxisType
        {
            LeftUp,
            RightUp,
            LeftDown,
            RightDown,
            Middle,
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

        [Ext.ReadOnlyInGame]
        public AxisType NavigateAxis;

        [Ext.ReadOnlyInGame]
        public NavigateModeType NavigateMode;

        // [Ext.ReadOnlyInGame]
        public Vector3 AppendPosition;

        // [Ext.ReadOnlyInGame]
        public Vector3 AppendPercent;

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
        private Ease AppendEase = Ease.OutSine;

        [SerializeField]
        private Ease DisappearEase = Ease.OutSine;

        [SerializeField]
        private bool disableAppendAnime = false;

        [SerializeField]
        private bool disableDisappearAnime = false;

        [SerializeField]
        private bool enableNavigator = true;

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
            if (!enableNavigator)
                return;

            switch (AppendType)
            {
                case AppendTypeEnum.Slide:
                    SlideAppend(AppendEdge);
                    break;
            }
        }

        public void Disappear()
        {
            if (!enableNavigator)
                return;

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
                .DOLocalMove(
                    FormatAxis(),
                    disableAppendAnime ? 0f : ParentPage.PageOpenAnimeDuration
                )
                .SetEase(AppendEase)
                .OnComplete(CompleteAppend);
        }

        private void SlideDisappear(CanvasEdge Edge)
        {
            DisappearTween = transform
                .DOLocalMove(
                    GetDisappearPoint(Edge),
                    disableDisappearAnime ? 0f : ParentPage.PageCloseAnimeDuration
                )
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
                    transform.SetLocalPositionX(FormatAxis().x);
                    break;

                case CanvasEdge.BottomEdge:
                    transform.SetLocalPositionY(-0.5f * Rect.height - SelfRect.rect.height);
                    transform.SetLocalPositionX(FormatAxis().x);
                    break;

                case CanvasEdge.LeftEdge:
                    transform.SetLocalPositionX(-0.5f * Rect.width - SelfRect.rect.width);
                    transform.SetLocalPositionY(FormatAxis().y);
                    break;

                case CanvasEdge.RightEdge:
                    transform.SetLocalPositionX(0.5f * Rect.width + SelfRect.rect.width);
                    transform.SetLocalPositionY(FormatAxis().y);
                    break;
            }

            return transform.localPosition;
        }

        private Vector3 GetDisappearPoint(CanvasEdge Edge)
        {
            Rect Rect = ResizeDetector.Rect.rect;

            return Edge switch
            {
                CanvasEdge.LeftEdge => new Vector3(
                    -0.5f * Rect.width - SelfRect.rect.width,
                    transform.localPosition.y,
                    0f
                ),
                CanvasEdge.RightEdge => new Vector3(
                    0.5f * Rect.width + SelfRect.rect.width,
                    transform.localPosition.y,
                    0f
                ),
                CanvasEdge.TopEdge => new Vector3(
                    transform.localPosition.x,
                    0.5f * Rect.height + SelfRect.rect.height,
                    0f
                ),
                CanvasEdge.BottomEdge => new Vector3(
                    transform.localPosition.x,
                    -0.5f * Rect.height - SelfRect.rect.height,
                    0f
                ),
                _ => throw new ArgumentOutOfRangeException(),
            };
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

        private Vector3 FormatAxis()
        {
            Rect Rect = ResizeDetector.Rect.rect;

            if (NavigateMode == NavigateModeType.Percent)
            {
                Vector3 position = AppendPercent;

                return NavigateAxis switch
                {
                    // Canvas 左上角为坐标 0 点，右下角为（1，1）
                    AxisType.LeftUp => new Vector3(
                        -0.5f * Rect.width + position.x * Rect.width,
                        0.5f * Rect.height - position.y * Rect.height,
                        position.z
                    ),
                    // Canvas 右上角为坐标 0 点，左下角为（1，1）
                    AxisType.RightUp => new Vector3(
                        0.5f * Rect.width - position.x * Rect.width,
                        0.5f * Rect.height - position.y * Rect.height,
                        position.z
                    ),
                    // Canvas 左下角为坐标 0 点，右上角为（1，1）
                    AxisType.LeftDown => new Vector3(
                        -0.5f * Rect.width + position.x * Rect.width,
                        -0.5f * Rect.height + position.y * Rect.height,
                        position.z
                    ),
                    // Canvas 右下角为坐标 0 点，左上角为（1，1）
                    AxisType.RightDown => new Vector3(
                        0.5f * Rect.width - position.x * Rect.width,
                        -0.5f * Rect.height + position.y * Rect.height,
                        position.z
                    ),
                    // Canvas 中心为坐标 0 点，右上角为（0.5，0.5），左下角为（-0.5，-0.5）
                    AxisType.Middle => new Vector3(
                        position.x * Rect.width,
                        position.y * Rect.height,
                        position.z
                    ),
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
            else if (NavigateMode == NavigateModeType.Axis)
            {
                Vector3 position = AppendPosition;

                return NavigateAxis switch
                {
                    // Axis模式下，AppendPosition是相对于锚点的相对坐标差
                    AxisType.LeftUp => new Vector3(
                        -0.5f * Rect.width + position.x,
                        0.5f * Rect.height - position.y,
                        position.z
                    ),
                    AxisType.RightUp => new Vector3(
                        0.5f * Rect.width - position.x,
                        0.5f * Rect.height - position.y,
                        position.z
                    ),
                    AxisType.LeftDown => new Vector3(
                        -0.5f * Rect.width + position.x,
                        -0.5f * Rect.height + position.y,
                        position.z
                    ),
                    AxisType.RightDown => new Vector3(
                        0.5f * Rect.width - position.x,
                        -0.5f * Rect.height + position.y,
                        position.z
                    ),
                    AxisType.Middle => new Vector3(position.x, position.y, position.z),
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            return Vector3.zero;
        }
    }
}
