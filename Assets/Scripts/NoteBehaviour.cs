using BaseObject;
using IUtils;
using UnityEngine;

namespace NBehaviour
{
    public class NoteBehaviour : BaseObjectBehaviour, IDev
    {
        public float JudgeTime;

        public bool isJudged;

        void Start()
        {
            SpriteRenderer.sprite = SpriteList[0];
        }

        private void OnJudge()
        {
            if (!isDestroy)
            {
                isDestroy = true;

                DestroyEvent?.Invoke();
            }
        }

        void Update() { }

        public new void DevLog()
        {
            ((BaseObjectBehaviour)this).DevLog();

            Debug.Log("Note Object: ");

            AnimeQueue.Peek().DevLog();
        }
    }
}
