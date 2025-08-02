using System;
using System.Collections.Generic;
using Singleton;
using UnityEngine;

namespace UserInterfaceNS
{
    public interface IPageControlled<T>
        where T : MonoBehaviour
    {
        public void OnOpenPage();
        public void OnClosePage();
    }

    public interface IPage<T>
        where T : MonoBehaviour
    {
        public void OpenPage();
        public void ClosePage();
    }

    public class UserInterfaceManager : Singleton<UserInterfaceManager>
    {
        public Stack<BaseUIPage> CurPage { get; private set; } = new();

        [SerializeField]
        public BaseUIPage GamePage;

        [SerializeField]
        public BaseUIPage AnotherPage;

        protected override void SingletonAwake()
        {
            GamePage = Instantiate(GamePage); // ���˲���
            AnotherPage = Instantiate(AnotherPage);
            InitWithPage(GamePage);
        }

        public void SwitchToPage(BaseUIPage Page)
        {
            if (CurPage != null)
            {
                CurPage.Peek().OnClosePage();

                if (CurPage.Count < 2)
                {
                    Debug.Log("ҳ����ջ");
                    CurPage.Push(Page);
                }
                else
                {
                    Debug.Log("ҳ���ջ");
                    CurPage.Pop();
                }

                CurPage.Peek().OnOpenPage();
            }
        }

        public void InitWithPage(BaseUIPage Page)
        {
            CurPage.Push(Page);
            CurPage.Peek().OnOpenPage();
        }

        public void Update()
        {
            CurPage.Peek().OnUpdatePage();
        }
    }
}
