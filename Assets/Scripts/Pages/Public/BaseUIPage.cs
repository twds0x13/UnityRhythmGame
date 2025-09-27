using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        protected List<Image> DisplayImages = new();

        private List<Color> ImageColors = new();

        [Ext.ReadOnlyInGame, SerializeField]
        private ResizeDetector ResizeDetector;

        private List<IPageControlled> ControlledObjects = new();

        public float PageOpenAnimeDuration { get; protected set; } = 0f;

        public float PageCloseAnimeDuration { get; protected set; } = 0f;

        public Action ActionFinishClosePage;

        public string Name { get; private set; }

#if UNITY_EDITOR

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
        /// 点击运行时自动检测并删除空引用，但是在发布版本中不应该留下空引用
        /// </summary>
        private void OnValidate()
        {
            DisplayTexts = DisplayTexts.Where(x => x != null).ToList();
            DisplaySelectables = DisplaySelectables.Where(x => x != null).ToList();
        }

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

            RegisterEventSystem();

            RegisterDisplayTexts();

            RegisterDisplayButtons();

            RegisterDisplayImages();
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

        private void RegisterDisplayButtons()
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
            await UniTask.WaitForSeconds(0f);

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

            UniTask.Void(DelayedClose);
        }

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
