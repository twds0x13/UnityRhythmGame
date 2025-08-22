using Anime;
using NoteStateMachine;
using PooledObjectNS;
using StateMachine;
using TrackNS;
using Game = GameManagerNS.GameManager;
using Judge = NoteJudgeNS.NoteJudge;

namespace NoteNS
{
    public class NoteBehaviour : PooledObjectBehaviour
    {
        private StateMachine<NoteBehaviour> StateMachine; // 动画状态机

        public StateInitNote InitNote; // 从动画状态开始更新

        public StateAnimeNote AnimeNote; // 从动画状态开始更新

        public StateJudgeAnimeNote JudgeNoteAnime; // 播放被击中的动画，本质是 Disappear 的分支

        public StateDisappearNote DisappearNoteAnime; // 如果没击中就处理消失动画逻辑

        public StateDestroyNote DestroyNoteAnime; // 击中或消失之后要删除note

        private StateMachine<NoteBehaviour> JudgeMachine; // 判定状态机

        public StateBeforeJudgeNote BeforeJudge; // 进入判定区之前

        public StateOnJudgeNote ProcessJudge; // 在判断区间内

        public StateAfterJudgeNote AfterJudge; // 过了判断区间

        public TrackBehaviour ParentTrack; // 归属的那一个轨道，获取 transform.position 作为动画坐标原点（默认情况落到轨道上）

        public float JudgeTime { get; private set; } // 预计判定时间

        public void InitStateMachine(NoteBehaviour Note)
        {
            StateMachine = new();
            InitNote = new(Note, StateMachine);
            AnimeNote = new(Note, StateMachine);
            JudgeNoteAnime = new(Note, StateMachine);
            DisappearNoteAnime = new(Note, StateMachine);
            DestroyNoteAnime = new(Note, StateMachine);

            JudgeMachine = new();
            BeforeJudge = new(Note, JudgeMachine);
            ProcessJudge = new(Note, JudgeMachine);
            AfterJudge = new(Note, JudgeMachine);

            JudgeMachine.InitState(BeforeJudge);
            StateMachine.InitState(InitNote);
        }

        private void Update() // 两个状态机
        {
            JudgeMachine.CurState?.Update();
            StateMachine.CurState?.Update();
        }

        public void OnJudge()
        {
            if (JudgeMachine.CurState == ProcessJudge)
            {
                Game.Inst.Score.Score += Judge.GetJudgeScore(Judge.GetJudgeEnum(this));

                JudgeMachine.SwitchState(AfterJudge);
                StateMachine.SwitchState(JudgeNoteAnime);
            }
        }

        public NoteBehaviour Init(AnimeMachine Machine, TrackBehaviour Track, float Time) // 在 Objectpool 中调用这个函数作为通用起手，保证每次调用都从这里开始
        {
            JudgeTime = Time;
            ParentTrack = Track;
            AnimeMachine = Machine;
            InitStateMachine(this);
            return this;
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
