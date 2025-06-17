using IUtils;
using NBehaviour;
using PooledObject;
using UnityEditor.UI;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

namespace TBehaviour
{
    /// <summary>
    /// 从 <see cref="NoteBehaviour"/> 复制粘贴再修改得到的亵渎造物。
    /// </summary>
    public class TrackBehaviour : PooledObjectBehaviour, IPooling, IDev
    {
        public bool isFake;

        public int TrackId;

        /// <summary>
        /// 如果你什么也不想加，不要用 <see cref="null"/> 关键字, 用 <see cref="PooledObjectBehaviour.Null"/>
        /// </summary>
        private void Awake() // 在Awake时间段就处理好委托，方便OnEnable重置状态（ResetSelfHandler）
        {
            LogicUpdator += Null;
            AnimeDisappearUpdator += Disappear;
            AnimeQueueUpdator += Position;
            OnEnableHandler += RegisterReset; // 切记，OnEnale的执行顺序在 ObjectPool 对他进行操作之前
            AwakeHandler += Null;
        }

        public void RegisterReset() // 有很多东西需要运行时初始化（比如从GetComponent得到的唯一 TrackId）
        {
            LogicUpdator -= InvokeDestroy; // 这个必须先初始化，清空已经用过的删除事件，不然刚生成顺手NotePool就给删了......

            // Debug.LogFormat("Pre:{0}", TrackId); // 在这里 TrackId 还未被运行时修改，继承上一次被Reset时的值

            isDestroying = false;
            hasDisappearingAnime = false;

            PostResetLock = false; // 强制等待全部 GetComponent<> 修改结束后再进行后初始化

            DisappearingTimeSpan = 0.1f;
            DisappearingTimeCache = 0f;
            DeathTimeSpan = 0.5f;

            SpriteRenderer.sprite = SpriteList[0];
            SpriteRenderer.color = new Color(1f, 1f, 1f);
            transform.position = new Vector3(10f, 0f, 0f);

            LogicUpdator += ResetTrack; // 后初始化扔到 Update 更新循环里面
        }

        public void ResetTrack()
        {
            if (PostResetLock)
            {
                // Debug.LogFormat("Post:{0}", TrackId); // 在这里 TrackId 保证已经被初始化，且每次启用该物件之后肯定会调用一次

                if (TrackId < 4)
                    isDestroyAble = false;

                LogicUpdator -= ResetTrack; // 只触发一次
            }
        }

        public void Position()
        {
            CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - CurAnime.StartT) / CurAnime.TotalTimeElapse(),
                1f
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
