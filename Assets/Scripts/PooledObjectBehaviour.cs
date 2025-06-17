using System;
using System.Collections.Generic;
using Anime;
using IUtils;
using UnityEngine;
using UnityEngine.Events;
using static PooledObject.PooledObjectBehaviour;
using Game = GameBehaviourManager.GameBehaviour;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour, IDev
    {
        public UnityEvent DestroyEvent = new UnityEvent();

        public UnityEvent InactivateEvent = new UnityEvent();

        public Action OnEnableHandler; // ÿ�α�����ʱ��������������ӿ�

        public Action AwakeHandler; // ��һ�α�����ʱ��������������ӿ�

        public Func<bool> AnimeDisappearUpdator; // �ڴ�����ʧ������ʱ��������������ӿ�������������

        public Action AnimeQueueUpdator; // �ڻ�δ��ʧ������������ʱ����������ӿ�������������

        // ��֤��һ�θ���֮�ڻ��ҽ������ �����������������ӿ�֮һ

        public Action LogicUpdator; // ������״̬�¶����ýӿڣ����ڴ����ж����������

        public SpriteRenderer SpriteRenderer;

        public Sprite[] SpriteList;

        public bool hasDisappearingAnime = true;

        public bool isDisappearing = false;

        public bool isDestroying = false;

        public bool isDestroyAble = true;

        public bool isInactive = false;

        public bool isStartAnime = false; // �Ƿ�ʼ��һ�ζ���

        public bool isEndAnime = false; // �Ƿ�������һ�ζ���

        public bool PostResetLock = false; // �Ƿ��ܽ��к��ʼ���� �ȴ�ȫ�� GetComponent<> �޸Ľ������ٽ��к��ʼ�� ��

        public System.Random Rand = new System.Random();

        public Queue<AnimeClip> AnimeQueue;

        public AnimeClip CurAnime;

        public float DisappearingTimeSpan;

        public float DisappearingTimeCache;

        public float DisappearingCurTCache;

        public Vector3 DisappearingPosCache;

        private float DeathTime;

        public float DeathTimeSpan // ��������ж������л������ʧ����֮��Ӧ�ٻ��ã����ڴ����ж���
        {
            get { return DeathTime; }
            protected set { DeathTime = Mathf.Clamp(value, 0f, float.PositiveInfinity); }
        }

        private float T;

        public float CurT
        {
            get { return T; }
            protected set { T = Mathf.Clamp(value, 0f, 1f); }
        }

        public bool IsVisableInCamera { get; private set; }

        public void Null() { } // It's Empty!

        private void OnBecameVisible()
        {
            IsVisableInCamera = true;
        }

        private void OnBecameInvisible()
        {
            IsVisableInCamera = false;
        }

        private void Awake()
        {
            AwakeHandler();
        }

        private void OnEnable()
        {
            isDestroying = false;

            isInactive = false;

            isDisappearing = false;

            OnEnableHandler();
        }

        public void Update()
        {
            AnimeUpdatorManager();
            LogicUpdator();
        }

        public void RegisterDestroy()
        {
            if (!isDestroying && isDestroyAble)
            {
                isDestroying = true;

                SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // ֻ������Ļ����ʾ���ж�ѭ�����ɹ���

                LogicUpdator += InvokeDestroy;
            }
        }

        public void InvokeDestroy()
        {
            if (!hasDisappearingAnime)
            {
                if (Game.Inst.GetGameTime() > DisappearingTimeCache + DeathTimeSpan) // ��������£��� Miss �ж���Χ��Ӧ�ó��� ��500ms ......
                {
                    DestroyEvent?.Invoke();
                }
            }
            else
            {
                if (DisappearingTimeSpan < DeathTimeSpan)
                {
                    if (
                        Game.Inst.GetGameTime()
                        > DisappearingTimeCache + DeathTimeSpan - DisappearingTimeSpan
                    )
                    {
                        DestroyEvent?.Invoke();
                    }
                }
                else
                {
                    DestroyEvent?.Invoke();
                }
            }
        }

        public void Inactivate()
        {
            if (!isInactive)
            {
                isInactive = true;

                InactivateEvent?.Invoke();
            }
        }

        /// <summary>
        /// ������Ҫ�����ĸ����������ӿڵ�״̬��
        /// </summary>
        public void AnimeUpdatorManager()
        {
            AnimeQueue.TryPeek(out CurAnime); // ����Ҫ��һ����׶���

            if (hasDisappearingAnime && isDisappearing)
            {
                if (!AnimeDisappearUpdator())
                {
                    RegisterDestroy(); // �ڴ�������ʧ������׼��ɾ��
                }
            }
            else
            {
                if (Game.Inst.GetGameTime() < CurAnime.EndT)
                {
                    AnimeQueueUpdator();
                }
                else
                {
                    if (!AnimeQueue.TryDequeue(out CurAnime))
                    {
                        if (hasDisappearingAnime)
                        {
                            DisappearingTimeCache = Game.Inst.GetGameTime(); // ֻ����ʧ������ʼǰִ��һ�Σ���ȡ��ʧ��ʼ˲��ļ�����Ҫ״̬

                            DisappearingPosCache = transform.position;

                            DisappearingCurTCache = 0f;

                            isDisappearing = true;
                        }
                        else
                        {
                            RegisterDestroy(); // ���û����ʧ��������׼��ɾ��
                        }
                    }
                }
            }
        }

        public void DevLog()
        {
            Debug.LogFormat("Pooled Object : ");
        }
    }
}
