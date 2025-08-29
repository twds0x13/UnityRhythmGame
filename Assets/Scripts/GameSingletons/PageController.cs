using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PageNS;
using Singleton;
using UnityEngine;

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
    public class PageController : Singleton<PageController>
    {
        public BaseUIPage CurPage { get; private set; }

        public List<BaseUIPage> HoverPageList { get; private set; }

        [SerializeField]
        Canvas BackGroundCanvas; // 公用背景图层 ( 渲染顺序 -1 ) 在切换到不同页面时操作这个图层内的内容

        [SerializeField]
        List<BaseUIPage> PageObjects; // 方便在 Inspector 里查看

        [SerializeField]
        List<BaseUIPage> HoverPageObjects; // 悬浮页面 (弹窗)

        public Dictionary<string, BaseUIPage> AllPages { get; private set; } = new();

        public Dictionary<string, BaseUIPage> AllHoverPages { get; private set; } = new();

#if UNITY_EDITOR

        public void AddPageObject(BaseUIPage Page)
        {
            PageObjects.Add(Page);
        }
#endif

        protected override void SingletonAwake()
        {
            BackGroundCanvas.gameObject.SetActive(true);

            RegisterObjectsFromRef();

            InitHoverPages();

            InitWithPage(nameof(InitPage));
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
        }

        public void FinishAllHoverPage()
        {
            foreach (var HoverPage in HoverPageList)
            {
                HoverPage.OnClosePage();
                HoverPageList.Remove(HoverPage);
            }
        }

        public void OpenHoverPage(string Name)
        {
            AllHoverPages[Name].OnOpenPage();

            HoverPageList.Add(AllHoverPages[Name]);
        }

        public void CloseHoverPage(string Name)
        {
            if (HoverPageList.Contains(AllHoverPages[Name]))
            {
                AllHoverPages[Name].OnClosePage();

                HoverPageList.Remove(AllHoverPages[Name]);
            }
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

            // 暂时没用

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
        }

        public Rect GetPageRect() => CurPage.GetRect();
    }
}
