using Anime;
using UnityEngine;
using UnityEngine.Events;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour // ������
    {
        public PooledObjectBehaviour Inst { get; private set; }

        public System.Random RandInst = new(); // ��ʱ���õ����

        public UnityEvent DestroyEvent = new(); // �ⲿ����

        public SpriteRenderer SpriteRenderer; // Object Pool ��Ԥ���嶼��Ҫ SpriteRenderer

        public AnimeMachine AnimeMachine; // �������г�ʼ��

        public Sprite[] SpriteList; // ��Ҫ�Ƿǿյ�

        private void Awake() // ��Ϊ���󱻶���ظ��ã�������ȫ����ֻ��ִ��һ�Σ�����Ҫ Init() ����������
        {
            if (Inst != this)
                Inst = this;
        }
    }
}
