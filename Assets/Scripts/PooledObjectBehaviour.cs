using Anime;
using UnityEngine;
using UnityEngine.Events;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour // 子类用
    {
        public PooledObjectBehaviour Instance { get; private set; }

        public System.Random RandInst = new(); // 随时能用的随机

        public UnityEvent DestroyEvent = new(); // 外部调用

        public SpriteRenderer SpriteRenderer; // 记得把所有需要 Object Pool 的预制体挂上 SpriteRenderer

        public AnimeMachine AnimeMachine; // 在子类中初始化

        public Sprite[] SpriteList; // 需要是非空的

        private void Awake() // 因为对象被对象池复用，所以在全局中只需执行一次，不需要 Init() 函数调用了
        {
            if (Instance != this)
                Instance = this;
        }
    }
}
