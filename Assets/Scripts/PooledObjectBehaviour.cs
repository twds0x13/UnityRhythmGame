using Anime;
using UnityEngine;
using UnityEngine.Events;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour // ������
    {
        public PooledObjectBehaviour Instance { get; private set; }

        public System.Random RandInst = new(); // ��ʱ���õ����

        public UnityEvent DestroyEvent = new(); // �ⲿ����

        public SpriteRenderer SpriteRenderer; // �ǵð�������Ҫ Object Pool ��Ԥ������� SpriteRenderer

        public AnimeMachine AnimeMachine; // �������г�ʼ��

        public Sprite[] SpriteList; // ��Ҫ�Ƿǿյ�

        private void Awake() // ��Ϊ���󱻶���ظ��ã�������ȫ����ֻ��ִ��һ�Σ�����Ҫ Init() ����������
        {
            if (Instance != this)
                Instance = this;
        }
    }
}
