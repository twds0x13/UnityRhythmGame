using System.Collections.Generic;
using Anime;
using PooledObject;
using StateMachine;
using TrackStateMachine;

namespace TrackManager
{
    /*
     * 历史性的一刻
     *
     * 我把Note相关的两个文件复制了一份
     *
     * 把里面所有的 "Note" 字符替换成了 "Track"
     *
     * 完全正常运行
     *
     * 望周知
     */

    public class TrackBehaviour : PooledObjectBehaviour
    {
        public StateMachine<TrackBehaviour> StateMachine; // 状态机

        public StateInitTrack InitTrack; // 从动画状态开始更新

        public StateAnimeTrack AnimeTrack; // 从动画状态开始更新

        public StateDisappearTrack DisappearTrack;

        public StateDestroyTrack DestroyTrack;

        public float JudgeTime;

        public bool isJudged = false;

        public bool isFake = false;

        public void InitStateMachine(TrackBehaviour Track)
        {
            StateMachine = new();
            InitTrack = new(Track, StateMachine);
            AnimeTrack = new(Track, StateMachine);
            DisappearTrack = new(Track, StateMachine);
            DestroyTrack = new(Track, StateMachine);

            StateMachine.InitState(InitTrack);
        }

        public void Update()
        {
            StateMachine.CurState?.Update();
        }

        public override void Init(AnimeMachine Machine) // 在 Objectpool 中调用这个函数作为通用起手，保证每次调用都从这里开始
        {
            base.Init(Machine);
            InitStateMachine(this);
        }

        public override PooledObjectBehaviour GetBase() // 这里没找到相关资料，只好先这么凑合了
        {
            return base.GetBase();
        }
    }
}
