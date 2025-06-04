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
            if (IsVisableInCamera)
            {
                AnimeQueue.TryPeek(out CurAnime);
                if (Game.Inst.GameTime() < CurAnime.EndT)
                {
                    CurT = CurTHandler(
                        (Game.Inst.GameTime() - CurAnime.StartT) / CurAnime.TotalTimeElapse(),
                        0.9f
                    );

                    transform.position = (1 - CurT) * CurAnime.StartV + CurT * CurAnime.EndV;
                }
                else
                {
                    AnimeQueue.TryDequeue(out CurAnime);
                }
            }
        }

        public float CurTHandler(float CurT, float I)
        {
            return Mathf.Pow(CurT, I);
        }

        public void DevLog()
        {
            Debug.Log("Base Object : \n");
        }
    }
}
