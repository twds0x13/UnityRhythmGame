using Anime;
using NoteStateMachine;
using PooledObject;
using StateMachine;
using TrackNamespace;
using UnityEngine;

namespace NoteNamespace
{
    public class NoteBehaviour : PooledObjectBehaviour
    {
        // 事实证明，定义几个变量比在状态机里加 Dictionary 然后用 SwitchTo(Enum State) 方便。

        public StateMachine<NoteBehaviour> StateMachine; // 状态机

        public StateInitNote InitNote; // 从动画状态开始更新

        public StateAnimeNote AnimeNote; // 从动画状态开始更新

        public StateScoreNote ScoreNote; // 如果击中就播放得分动画，音效等

        public StateDisappearNote DisappearNote; // 如果没击中就处理消失动画逻辑

        public StateDestroyNote DestroyNote; // 击中或消失之后要删除note

        public TrackBehaviour ParentTrack; // 归属的那一个轨道，获取 transform.position 作为动画偏移量（默认情况落到轨道上）

        public void InitStateMachine(NoteBehaviour Note)
        {
            StateMachine = new();
            InitNote = new(Note, StateMachine);
            AnimeNote = new(Note, StateMachine);
            ScoreNote = new(Note, StateMachine);
            DisappearNote = new(Note, StateMachine);
            DestroyNote = new(Note, StateMachine);

            StateMachine.InitState(InitNote);
        }

        public void Update()
        {
            StateMachine.CurState?.Update();
        }

        public void Init(AnimeMachine Machine, TrackBehaviour Track) // 在 Objectpool 中调用这个函数作为通用起手，保证每次调用都从这里开始
        {
            ParentTrack = Track;
            AnimeMachine = Machine;
            InitStateMachine(this);
        }
    }
}
