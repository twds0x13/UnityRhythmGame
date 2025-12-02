using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NavigatorNS;
using UIEventSystemNS;
using UIManagerNS;
using UnityEngine;
using UnityEngine.UI;
using PageManager = UIManagerNS.PageManager;

namespace PageNS
{
    [
        RequireComponent(typeof(Canvas)),
        RequireComponent(typeof(BaseUIEventSystem)),
        RequireComponent(typeof(RectTransform))
    ]
    public abstract class BaseUIPage : MonoBehaviour
    {
        protected PageManager Manager { get; private set; } // 好文明

        [Ext.ReadOnlyInGame, SerializeField]
        protected BaseUIEventSystem EventSystem;

        [Ext.ReadOnlyInGame, SerializeField]
        private RectTransform SelfRect;

        [Ext.ReadOnlyInGame, SerializeField]
        protected List<TextDisplay> DisplayTexts = new();

        [Ext.ReadOnlyInGame, SerializeField]
        protected List<SelectableDisplay> DisplaySelectables = new();

        [Ext.ReadOnlyInGame, SerializeField]
        protected List<ScrollDisplay> DisplayScroll = new();

        [Ext.ReadOnlyInGame, SerializeField]
        protected List<Image> DisplayImages = new();

        [Ext.ReadOnlyInGame, SerializeField]
        protected List<UINavigator> DisplayNavigator = new();

        private readonly List<Color> ImageColors = new();

        [Ext.ReadOnlyInGame, SerializeField]
        private ResizeDetector ResizeDetector;

        private readonly List<IPageControlled> ControlledObjects = new();

        private readonly List<UINavigator> ControlledNavigators = new();

        public float PageOpenAnimeDuration { get; protected set; } = 0f;

        public float PageCloseAnimeDuration { get; protected set; } = 0f;

        public Action ActionFinishClosePage;

        public string Name { get; private set; }

#if UNITY_EDITOR

        /// <summary>
        /// 只在自动创建模板 Page 时使用
        /// </summary>
        /// <param name="Component"></param>
        public void RegisterComponent(IPageComponent Component)
        {
            if (Component is not null)
            {
                switch (Component)
                {
                    case TextDisplay:
                        DisplayTexts.Add(Component as TextDisplay);
                        break;
                    case SelectableDisplay:
                        DisplaySelectables.Add(Component as SelectableDisplay);
                        break;
                }
            }
        }

        /// <summary>
        /// 只在自动创建模板 Page 时使用
        /// </summary>
        /// <param name="Detector"></param>
        public void SetResizeDetector(ResizeDetector Detector)
        {
            ResizeDetector = Detector;
        }
#endif

        protected void SetName(string Name)
        {
            this.Name = Name;
        }

        public virtual void OnAwake()
        {
            Manager = PageManager.Inst;

            ResizeDetector = ResizeDetector.Inst;

            RegisterEventSystem();

            RegisterDisplayTexts();

            RegisterDisplaySelectables();

            RegisterDisplayScrolls();

            RegisterDisplayImages();

            RegisterDisplayNavigators();
        }

        private void RegisterEventSystem()
        {
            EventSystem.OnAwake();

            RegisterObject(EventSystem);
        }

        private void RegisterDisplayTexts()
        {
            for (int i = 0; i < DisplayTexts.Count; i++)
            {
                DisplayTexts[i].OnAwake();

                DisplayTexts[i].SetParentPage(this);

                DisplayTexts[i].SetResizeDetector(ResizeDetector);

                RegisterObject(DisplayTexts[i]);
            }
        }

        private void RegisterDisplaySelectables()
        {
            for (int i = 0; i < DisplaySelectables.Count; i++)
            {
                DisplaySelectables[i].OnAwake();

                DisplaySelectables[i].SetParentPage(this);

                DisplaySelectables[i].SetResizeDetector(ResizeDetector);

                RegisterObject(DisplaySelectables[i]);
            }
        }

        private void RegisterDisplayImages()
        {
            for (int i = 0; i < DisplayImages.Count; i++)
            {
                ImageColors.Add(DisplayImages[i].color);

                DisplayImages[i].gameObject.SetActive(false);
            }
        }

        private void RegisterDisplayScrolls()
        {
            for (int i = 0; i < DisplayScroll.Count; i++)
            {
                DisplayScroll[i].OnAwake();
                DisplayScroll[i].SetParentPage(this);
                DisplayScroll[i].SetResizeDetector(ResizeDetector);
                RegisterObject(DisplayScroll[i]);
            }
        }

        private void RegisterDisplayNavigators()
        {
            for (int i = 0; i < DisplayNavigator.Count; i++)
            {
                DisplayNavigator[i].Init(this);

                DisplayNavigator[i].SetResizeDetector(ResizeDetector);

                ControlledNavigators.Add(DisplayNavigator[i]);
            }
        }

        public void SelectFirstAfterOneFrame() => EventSystem.SelectFirstAfterOneFrame().Forget();

        public Image GetDisplayImage(int num) => DisplayImages[num];

        public Image FindDisplayImage(string name) =>
            DisplayImages.Find(image => image.name == name);

        public virtual void OnOpenPage()
        {
            gameObject.SetActive(true);

            UniTask.Void(DelayedOpen);
        }

        private async UniTaskVoid DelayedOpen() // 如果之后也用不到再删除，如果用到了就加变量控制
        {
            await UniTask.Yield();

            if (ControlledObjects.Count > 0)
            {
                foreach (IPageControlled Object in ControlledObjects)
                {
                    Object.OnOpenPage();
                }
            }

            if (DisplayImages.Count > 0)
            {
                for (int i = 0; i < DisplayImages.Count; i++)
                {
                    DisplayImages[i].gameObject.SetActive(true);

                    DisplayImages[i].color = new Color(0f, 0f, 0f, 0f);

                    DisplayImages[i].DOColor(ImageColors[i], PageOpenAnimeDuration);
                }
            }

            if (ControlledNavigators.Count > 0)
            {
                foreach (UINavigator Navigator in ControlledNavigators)
                {
                    Navigator.Append();
                }
            }
        }

        public virtual void OnUpdatePage() { }

        public virtual void OnClosePage()
        {
            if (ControlledObjects.Count > 0)
            {
                foreach (IPageControlled Object in ControlledObjects)
                {
                    Object.OnClosePage();
                }
            }

            if (DisplayImages.Count > 0)
            {
                for (int i = 0; i < DisplayImages.Count; i++)
                {
                    DisplayImages[i].DOColor(new Color(0f, 0f, 0f, 0f), PageCloseAnimeDuration);
                }
            }

            if (ControlledNavigators.Count > 0)
            {
                foreach (UINavigator Navigator in ControlledNavigators)
                {
                    Navigator.Disappear();
                }
            }

            UniTask.Void(DelayedClose);
        }

        public virtual void OnDestroyPage() { }

        public void RegisterObject(IPageControlled Controlled)
        {
            ControlledObjects.Add(Controlled);
        }

        public void UnregisterObject(IPageControlled Controlled)
        {
            ControlledObjects.Remove(Controlled);
        }

        public Rect GetRect() => SelfRect.rect;

        public async UniTaskVoid DelayedClose() // 这个很有用
        {
            await UniTask.WaitForSeconds(PageCloseAnimeDuration);

            ActionFinishClosePage?.Invoke();

            gameObject.SetActive(false);
        }
    }
}
