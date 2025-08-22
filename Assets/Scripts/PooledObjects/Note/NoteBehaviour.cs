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
        private StateMachine<NoteBehaviour> StateMachine; // ����״̬��

        public StateInitNote InitNote; // �Ӷ���״̬��ʼ����

        public StateAnimeNote AnimeNote; // �Ӷ���״̬��ʼ����

        public StateJudgeAnimeNote JudgeNoteAnime; // ���ű����еĶ����������� Disappear �ķ�֧

        public StateDisappearNote DisappearNoteAnime; // ���û���оʹ�����ʧ�����߼�

        public StateDestroyNote DestroyNoteAnime; // ���л���ʧ֮��Ҫɾ��note

        private StateMachine<NoteBehaviour> JudgeMachine; // �ж�״̬��

        public StateBeforeJudgeNote BeforeJudge; // �����ж���֮ǰ

        public StateOnJudgeNote ProcessJudge; // ���ж�������

        public StateAfterJudgeNote AfterJudge; // �����ж�����

        public TrackBehaviour ParentTrack; // ��������һ���������ȡ transform.position ��Ϊ��������ԭ�㣨Ĭ������䵽����ϣ�

        public float JudgeTime { get; private set; } // Ԥ���ж�ʱ��

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

        private void Update() // ����״̬��
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

        public NoteBehaviour Init(AnimeMachine Machine, TrackBehaviour Track, float Time) // �� Objectpool �е������������Ϊͨ�����֣���֤ÿ�ε��ö������￪ʼ
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
