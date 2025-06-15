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

        public AnimeUpdateHandler UpdateSelfDisappear; // 在处于消失过程中时，调用这个函数接口用作动画更新

        public UpdateHandler UpdateSelfPosition; // 在还未消失（正常动画）时，调用这个接口用作动画更新

        // 保证在一次更新之内会且仅会调用这两个动画接口之一

        public UpdateHandler RegisteredUpdates; // 在所有状态下都调用接口，用于处理判定等物理更新

        public SpriteRenderer SpriteRenderer;

        public Sprite[] SpriteList;

        public bool hasDisappearingAnime = true;

        public bool isDisappearing = false;

        public bool isDestroying = false;

        public bool isInactivate = false;

        public bool StartAnime = false; // 是否开始第一次动画

        public bool EndAnime = false; // 是否结束最后一次动画

        public System.Random Rand = new System.Random();

        public Queue<AnimeClip> AnimeQueue;

        public AnimeClip CurAnime;

        public float DisappearingTimeSpan;

        public float DisappearingTimeCache;

        public float DisappearingCurTCache;

        public Vector3 DisappearingPosCache;

        private float DeathTime;

        public float DeathTimeSpan // 在完成所有动画序列或完成消失动画之后应再活多久（用于处理判定）
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

                SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // 只隐藏屏幕内显示，判定循环依旧工作

                RegisteredUpdates += InvokeDestroy;
            }
        }

        public void InvokeDestroy()
        {
            if (!hasDisappearingAnime)
            {
                if (Game.Inst.GetGameTime() > DisappearingTimeCache + DeathTimeSpan) // 正常情况下，非 Miss 判定范围不应该超过 ±500ms ......
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
        /// 管理需要调用哪个函数接口的状态机
        /// </summary>
        public void HandlerManager()
        {
            AnimeQueue.TryPeek(out CurAnime);

            if (hasDisappearingAnime && isDisappearing)
            {
                if (!UpdateSelfDisappear())
                {
                    RegisterDestroy(); // 在处理完消失动画后准备删除
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
                            DisappearingTimeCache = Game.Inst.GetGameTime(); // 只在消失动画开始前执行一次，获取消失开始瞬间的几个必要状态

                            DisappearingPosCache = transform.position;

                            DisappearingCurTCache = 0f;

                            isDisappearing = true;
                        }
                        else
                        {
                            RegisterDestroy(); // 如果没有消失动画，就准备删除
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
