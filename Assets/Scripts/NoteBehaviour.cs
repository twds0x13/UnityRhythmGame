using Anime;
using IUtils;
using PooledObject;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

namespace NBehaviour
{
    public class NoteBehaviour : PooledObjectBehaviour, IPooling, IDev
    {
        public float JudgeTime;

        public bool isJudged;

        void Start()
        {
            SpriteRenderer.sprite = SpriteList[0];
        }

        private void Judge()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                OnDestroy();
            }
        }

        public void Update()
        {
            AnimeUpdate();
            Judge();
        }

        public new void DevLog()
        {
            base.DevLog();

            Debug.Log("Note Object: ");

            AnimeQueue.TryPeek(out CurAnime);

            CurAnime.DevLog();
        }
    }
}
