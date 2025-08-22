using Anime;
using UIManagerNS;
using UnityEngine;
using UnityEngine.Events;

namespace PooledObjectNS
{
    public class PooledObjectBehaviour : MonoBehaviour, IPageControlled
    {
        public PooledObjectBehaviour Inst { get; private set; }

        public System.Random RandInst = new(); // ��ʱ���õ����

        public UnityEvent DestroyEvent = new(); // �ⲿ����

        public SpriteRenderer SpriteRenderer; // Object Pool ��Ԥ���嶼��Ҫ SpriteRenderer

        public AnimeMachine AnimeMachine; // �������г�ʼ��

        public Sprite[] SpriteList; // ��Ҫ�Ƿǿյ�

        private void Awake() // �����������ʱ����
        {
            if (Inst != this)
                Inst = this;
        }

        public virtual void OnAwake() { }

        public virtual void OnOpenPage() { }

        public virtual void OnClosePage() { }
    }
}
