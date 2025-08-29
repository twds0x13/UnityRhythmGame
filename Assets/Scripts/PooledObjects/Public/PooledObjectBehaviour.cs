using Anime;
using UIManagerNS;
using UnityEngine;
using UnityEngine.Events;

namespace PooledObjectNS
{
    public class PooledObjectBehaviour : MonoBehaviour, IPageControlled
    {
        public PooledObjectBehaviour Inst { get; private set; }

        public System.Random RandInst = new(); // 随时能用的随机

        public UnityEvent DestroyEvent = new(); // 外部调用

        public SpriteRenderer SpriteRenderer; // Object Pool 的预制体都需要 SpriteRenderer

        public AnimeMachine AnimeMachine; // 在子类中初始化

        public Sprite[] SpriteList; // 需要是非空的

        private void Awake() // 这个不能运行时生成
        {
            if (Inst != this)
                Inst = this;
        }

        public virtual void OnAwake() { }

        public virtual void OnOpenPage() { }

        public virtual void OnClosePage() { }
    }
}
