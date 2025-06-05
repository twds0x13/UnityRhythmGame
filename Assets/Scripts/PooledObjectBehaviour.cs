using System.Collections.Concurrent;
using Anime;
using IUtils;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Game = GameBehaviourManager.GameBehaviour;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour, IDev
    {
        public UnityEvent DestroyEvent = new UnityEvent();

        public SpriteRenderer SpriteRenderer;

        public Sprite[] SpriteList;

        public bool isDestroy;

        public bool StartAnime = false; // 是否开始第一次动画

        public bool EndAnime = false; // 是否结束最后一次动画

        public ConcurrentQueue<AnimeClip> AnimeQueue;

        public AnimeClip CurAnime;

        private float T;

        public float CurT
        {
            get { return T; }
            private set { T = Mathf.Clamp(value, 0f, 1f); }
        }

        public bool IsVisableInCamera { get; private set; }

        public void Awake()
        {
            transform.position = new Vector3(0f, 3f, 0f);
        }

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
            isDestroy = false;
        }

        public void OnDestroy()
        {
            if (!isDestroy)
            {
                isDestroy = true;

                DestroyEvent?.Invoke();
            }
        }

        public void AnimeUpdate()
        {
            AnimeQueue.TryPeek(out CurAnime);

            if (Game.Inst.GetGameTime() < CurAnime.StartT && !StartAnime) // 在第一个动画节点之前移出屏幕
            {
                transform.position = new Vector3(10f, 10f, 10f);
            }
            else if (!StartAnime && Game.Inst.GetGameTime() > CurAnime.StartT)
            {
                StartAnime = true;
            }
            else if (!EndAnime)
            {
                StartAnime = true;
                if (Game.Inst.GetGameTime() < CurAnime.EndT)
                {
                    CurT = CurTHandler(
                        (Game.Inst.GetGameTime() - CurAnime.StartT) / CurAnime.TotalTimeElapse(),
                        1f
                    );

                    transform.position = (1 - CurT) * CurAnime.StartV + CurT * CurAnime.EndV;
                }
                else
                {
                    if (!AnimeQueue.TryDequeue(out CurAnime))
                    {
                        EndAnime = true;
                    }
                }
            }
            else if (EndAnime) // 在最后一个动画节点之后移出屏幕
            {
                transform.position = new Vector3(10f, 10f, 10f);
            }
        }

        public float CurTHandler(float CurT, float I)
        {
            return Mathf.Pow(CurT, I);
        }

        public void DevLog()
        {
            Debug.LogFormat("Pooled Object : {0}", );

        }
    }
}
