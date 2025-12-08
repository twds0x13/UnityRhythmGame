using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PageNS;
using Singleton;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIManagerNS
{
    public interface IPageControlled
    {
        public void OnAwake();
        public void OnOpenPage();
        public void OnClosePage();
    }

    public interface IPageComponent : IPageControlled
    {
        public void SetParentPage(BaseUIPage Page);

        public void SetResizeDetector(ResizeDetector Detector);
    }

    public interface IPage<T>
        where T : MonoBehaviour
    {
        public void OpenPage();
        public void ClosePage();
    }

    /// <summary>
    /// 基本就是状态机 UI 版
    /// </summary>
    public class PageManager : Singleton<PageManager>
    {
        public BaseUIPage CurPage { get; private set; }

        public BaseUIPage HoverPage { get; private set; }

        [Ext.ReadOnlyInGame, SerializeField]
        Canvas BackGroundCanvas; // 公用背景图层 ( 渲染顺序 -1 ) 在切换到不同页面时操作这个图层内的内容

        [Ext.ReadOnlyInGame]
        public List<BaseUIPage> PageObjects; // 方便在 Inspector 里查看

        [Ext.ReadOnlyInGame]
        public List<BaseUIPage> HoverPageObjects; // 悬浮页面 (弹窗)

        // [Ext.ReadOnlyInGame]
        // public List<Image> ImageObjects; // 页面图像

        public readonly Dictionary<string, BaseUIPage> AllPages = new();

        public readonly Dictionary<string, BaseUIPage> AllHoverPages = new();

        public readonly Dictionary<string, Image> AllImages = new();

#if UNITY_EDITOR

        // 方便在编辑器模式下由代码添加页面

        public void AddPageObject(BaseUIPage Page)
        {
            PageObjects.Add(Page);
        }

        public void AddHoverPageObject(BaseUIPage Page)
        {
            HoverPageObjects.Add(Page);
        }
#endif

        protected override void SingletonAwake()
        {
            BackGroundCanvas.gameObject.SetActive(true);

            RegisterObjectsFromRef();

            InitHoverPages();

            InitWithPage(nameof(InitPage));
        }

        protected override void SingletonDestroy()
        {
            foreach (var (_, page) in AllPages)
            {
                page.OnDestroyPage();
            }

            AllPages.Clear();
        }

        private void RegisterObjectsFromRef()
        {
            for (int i = 0; i < PageObjects.Count; i++)
            {
                PageObjects[i].OnAwake();

                AllPages.Add(PageObjects[i].Name, PageObjects[i]);
            }

            for (int i = 0; i < HoverPageObjects.Count; i++)
            {
                HoverPageObjects[i].OnAwake();

                AllHoverPages.Add(HoverPageObjects[i].Name, HoverPageObjects[i]);
            }

            for (int i = 0; i < PageObjects.Count; i++)
            {
                // AllImages.Add(ImageObjects[i].name, ImageObjects[i]);
            }
        }

        public void OpenHoverPage(string Name)
        {
            if (AllHoverPages.ContainsKey(Name))
            {
                HoverPage = AllHoverPages[Name];
                HoverPage.OnOpenPage();
            }
        }

        public void CloseHoverPage(string Name)
        {
            if (AllHoverPages.ContainsKey(Name) && HoverPage.Name == AllHoverPages[Name].Name)
            {
                HoverPage.OnClosePage();
                HoverPage = null;

                CurPage.SelectFirstAfterOneFrame();
            }
        }

        public void SwitchToPage<T>()
            where T : BaseUIPage
        {
            CurPage.OnClosePage();

            CurPage = AllPages[typeof(T).Name];

            UniTask.Void(DelayedOpen);
        }

        public void SwitchToPage(string Name)
        {
            CurPage.OnClosePage();

            CurPage = AllPages[Name];

            UniTask.Void(DelayedOpen);
        }

        public void JumpToPage(string Name) // OnClosePage 触发退出动画，然后由退出动画的 OnCompleted 触发这个函数，所以不应该重复触发 OnClosePage
        {
            CurPage = AllPages[Name];

            UniTask.Void(DelayedOpen);
        }

        public void InitHoverPages()
        {
            foreach (string HoverName in AllHoverPages.Keys)
            {
                AllHoverPages[HoverName].gameObject.SetActive(false);
            }
        }

        public void InitWithPage(string Name)
        {
            foreach (string PageName in AllPages.Keys)
            {
                if (PageName == Name)
                {
                    CurPage = AllPages[Name];
                }
                else
                {
                    AllPages[PageName].gameObject.SetActive(false);
                }
            }

            // 巧妙的使用一个中间页面来假装看不到开头卡顿

            // 暂时废弃了

            // 因为现在开头不卡了 ( 更巧妙的 InitPage )

            UniTask.Void(DelayedOpen);
        }

        private async UniTaskVoid DelayedOpen()
        {
            await UniTask.WaitForSeconds(0.00f);
            CurPage.OnOpenPage();
        }

        private void Update()
        {
            CurPage.OnUpdatePage();

            /*
            if (HoverPageList.Count > 0)
            {
                for (int i = 0; i < HoverPageList.Count; i++)
                {
                    HoverPageList[i].OnUpdatePage();
                }
            }
            */
        }

        public Rect GetPageRect() => CurPage.GetRect();
    }
}
