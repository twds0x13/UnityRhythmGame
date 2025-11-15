using System.Collections.Generic;
using Anime;
using NoteStateMachine;
using Parser;
using PooledObjectNS;
using StateMachine;
using TrackNS;
using Game = GameManagerNS.GameManager;
using Judge = JudgeNS.NoteJudge;

namespace NoteNS
{
    public class NoteBehaviour : PooledObjectBehaviour, IChartObject, IVertical
    {
        private LinearStateMachine<NoteBehaviour> StateMachine; // 动画状态机

        public StateInitNote InitNote; // 从动画状态开始更新

        public StateAnimeNote AnimeNote; // 从动画状态开始更新

        public StateJudgeAnimeNote JudgeNoteAnime; // 播放被击中的动画，本质是 Disappear 的分支

        public StateDisappearNote DisappearNoteAnime; // 如果没击中就处理消失动画逻辑

        public StateDestroyNote DestroyNoteAnime; // 击中或消失之后要删除note

        private LinearStateMachine<NoteBehaviour> JudgeMachine; // 判定状态机

        public StateBeforeJudgeNote BeforeJudge; // 进入判定区之前

        public StateOnJudgeNote OnJudge; // 在判断区间内

        public StateAfterJudgeNote AfterJudge; // 过了判断区间

        public TrackBehaviour ParentTrack; // 归属的那一个轨道，获取 transform.position 作为动画坐标原点（默认情况落到轨道上）

        public float JudgeTime { get; private set; } // 预计判定时间

        public float Vertical { get; set; } = 1f; // 纵向位置缩放

        public void InitStateMachine(NoteBehaviour Note)
        {
            StateMachine = new();

            InitNote = new(Note, StateMachine);
            AnimeNote = new(Note, StateMachine);
            JudgeNoteAnime = new(Note, StateMachine);
            DisappearNoteAnime = new(Note, StateMachine);
            DestroyNoteAnime = new(Note, StateMachine);

            // 正常的状态跳转用的都是 SwitchState
            // 如果想在 Init 前面临时插一段进去就用 LinearStateMachine 的方法调用 NextState 函数
            // 通用状态机写起来太麻烦了 小修小补可以

            var AnimeList = new List<IState<NoteBehaviour>> { InitNote };

            JudgeMachine = new();

            BeforeJudge = new(Note, JudgeMachine);
            OnJudge = new(Note, JudgeMachine);
            AfterJudge = new(Note, JudgeMachine);

            var JudgeList = new List<IState<NoteBehaviour>> { BeforeJudge };

            StateMachine.InitLinear(AnimeList);

            JudgeMachine.InitLinear(JudgeList);
        }

        private void Update() // 两个状态机
        {
            JudgeMachine.CurState?.Update();
            StateMachine.CurState?.Update();
        }

        public void OnPress()
        {
            if (JudgeMachine.CurState == OnJudge)
            {
                Game.Inst.Score.Score += Judge.GetJudgeScore(Judge.GetJudgeEnum(this));

                JudgeMachine.SwitchState(AfterJudge);
                StateMachine.SwitchState(JudgeNoteAnime);
            }
        }

        public void OnRelease() { }

        public NoteBehaviour Init(AnimeMachine Machine, TrackBehaviour Track, float Time) // 在 Objectpool 中调用这个函数，保证每次调用都从这里开始
        {
            JudgeTime = Time;
            ParentTrack = Track;
            AnimeMachine = Machine;
            InitStateMachine(this);
            return this;
        }

        public void ResetNote()
        {
            JudgeTime = 0f;
            ParentTrack = null;
            AnimeMachine = null;

            Vertical = 1f;

            StateMachine = null;
            JudgeMachine = null;
        }

        public override void OnClosePage()
        {
            if (StateMachine.CurState != JudgeNoteAnime)
            {
                StateMachine.SwitchState(DisappearNoteAnime);
            }

            JudgeMachine.SwitchState(AfterJudge);
        }
    }
}
