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

        private void Awake() // 在Awake时间段就处理好委托，方便OnEnable重置状态（ResetSelfHandler）
        {
            LogicUpdator += Judge;
            AnimeDisappearUpdator += Disappear;
            AnimeQueueUpdator += Position;
            OnEnableHandler += ResetNote; // 委托,极为先进的,,,,,,
        }

        public void Judge()
        {
            if (Input.GetKeyDown(KeyCode.T)) // 串接到轨道Object对应的属性
            {
                RegisterDestroy();
            }
        }

        public void ResetNote()
        {
            LogicUpdator -= InvokeDestroy; // 记得清空已经用过的删除事件，不然刚生成顺手NotePool就给删了......

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
                2f
            );

            transform.position = (1 - CurT) * CurAnime.StartV + CurT * CurAnime.EndV;
        }

        public bool Disappear()
        {
            CurT = DisappearingTimeCache + DisappearingTimeSpan - Game.Inst.GetGameTime(); // 0.2f -> 0f

            transform.position =
                DisappearingPosCache - new Vector3(0f, 3 * (DisappearingTimeSpan - CurT), 0f);

            SpriteRenderer.color = new Color(1f, 1f, 1f, CurT);

            return CurT != 0f;
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
