using Anime;
using PooledObject;
using StateMachine;
using TrackStateMachine;
using UnityEngine;

namespace TrackNamespace
{
    public class TrackBehaviour : PooledObjectBehaviour
    {
        public StateMachine<TrackBehaviour> StateMachine; // 状态机

        public StateInitTrack InitTrack; // 从动画状态开始更新

        public StateAnimeTrack AnimeTrack; // 从动画状态开始更新

        public StateDisappearTrack DisappearTrack;

        public StateDestroyTrack DestroyTrack;

        public int TrackNumber { get; private set; }

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

        public Vector3 CurPos()
        {
            return transform.position;
        }

        public void Init(AnimeMachine Machine, int Number) // 在 Objectpool 中调用这个函数作为通用起手，保证每次调用都从这里开始
        {
            TrackNumber = Number;
            AnimeMachine = Machine;
            InitStateMachine(this);
        }
    }
}
