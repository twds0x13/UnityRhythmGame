using Anime;
using IUtils;
using PooledObject;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

namespace NBehaviour
{
    public class NoteBehaviour : PooledObjectBehaviour, IPooling, IDev
    {
        public float JudgeTime;

        public bool isJudged;

        public bool isFake;

        void Start()
        {
            RegisteredUpdates += Judge;
            UpdateSelfDisappear += Disappear;
            UpdateSelfPosition += Position;
        }

        public void Judge()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                RegisterDestroy();
            }
        }

        public void ResetNote()
        {
            RegisteredUpdates -= InvokeDestroy; // 记得清空已经用过的删除事件，不然刚生成顺手NotePool就给删了......

            isDestroying = false;
            isJudged = false;
            hasDisappearingAnime = true;

            DisappearingTimeSpan = 0.1f;
            DisappearingTimeCache = 0f;
            DeathTimeSpan = 0.5f;

            SpriteRenderer.sprite = SpriteList[Rand.Next(0, 2)];
            SpriteRenderer.color = new Color(1f, 1f, 1f);
            transform.position = new Vector3(10f, 0f, 0f);
        }

        public void Position()
        {
            CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - CurAnime.StartT) / CurAnime.TotalTimeElapse(),
                3f
            );

            transform.position = (1 - CurT) * CurAnime.StartV + CurT * CurAnime.EndV;
        }

        public bool Disappear()
        {
            CurT = DisappearingTimeCache + DisappearingTimeSpan - Game.Inst.GetGameTime(); // 0.2f -> 0f

            transform.position =
                DisappearingPosCache - new Vector3(0f, 5 * (DisappearingTimeSpan - CurT), 0f);

            SpriteRenderer.color = new Color(1f, 1f, 1f, CurT);

            if (CurT == 0f)
            {
                return false;
            }
            else
            {
                return true;
            }
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
