using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Anime;
using IUtils;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Game = GameBehaviourManager.GameBehaviour;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour, IDev
    {
        public UnityEvent DestroyEvent = new UnityEvent();

        public UnityEvent InactivateEvent = new UnityEvent();

        public delegate void UpdateHandler();

        public delegate bool AnimeUpdateHandler();

        public AnimeUpdateHandler UpdateSelfDisappear; // �ڴ�����ʧ������ʱ��������������ӿ�������������

        public UpdateHandler UpdateSelfPosition; // �ڻ�δ��ʧ������������ʱ����������ӿ�������������

        // ��֤��һ�θ���֮�ڻ��ҽ�����������������ӿ�֮һ

        public UpdateHandler RegisteredUpdates; // ������״̬�¶����ýӿڣ����ڴ����ж����������

        public SpriteRenderer SpriteRenderer;

        public Sprite[] SpriteList;

        public bool hasDisappearingAnime = true;

        public bool isDisappearing = false;

        public bool isDestroying = false;

        public bool isInactivate = false;

        public bool StartAnime = false; // �Ƿ�ʼ��һ�ζ���

        public bool EndAnime = false; // �Ƿ�������һ�ζ���

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

        private void OnBecameVisible()
        {
            IsVisableInCamera = true;
        }

        private void OnBecameInvisible()
        {
            IsVisableInCamera = false;
        }

        public void OnEnable()
        {
            isDestroying = false;

            isInactivate = false;

            isDisappearing = false;
        }

        public void Update()
        {
            HandlerManager();
            RegisteredUpdates();
        }

        public void RegisterDestroy()
        {
            if (!isDestroying)
            {
                isDestroying = true;

                SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // ֻ������Ļ����ʾ���ж�ѭ�����ɹ���

                RegisteredUpdates += InvokeDestroy;
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
            if (!isInactivate)
            {
                isInactivate = true;

                InactivateEvent?.Invoke();
            }
        }

        /// <summary>
        /// ������Ҫ�����ĸ������ӿڵ�״̬��
        /// </summary>
        public void HandlerManager()
        {
            AnimeQueue.TryPeek(out CurAnime);

            if (hasDisappearingAnime && isDisappearing)
            {
                if (!UpdateSelfDisappear())
                {
                    RegisterDestroy(); // �ڴ�������ʧ������׼��ɾ��
                }
            }
            else
            {
                if (Game.Inst.GetGameTime() < CurAnime.EndT)
                {
                    UpdateSelfPosition();
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
