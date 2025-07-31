using System;
using Anime;
using NoteStateMachine;
using PooledObject;
using StateMachine;
using TrackNamespace;
using UnityEngine.InputSystem;
using Judge = NoteJudge.NoteJudge;

namespace NoteNamespace
{
    public class NoteBehaviour : PooledObjectBehaviour
    {
        // ��ʵ֤�������弸����������״̬����� Dictionary Ȼ���� SwitchTo(Enum State) ���㡣

        private StateMachine<NoteBehaviour> StateMachine; // ����״̬��

        public StateInitNote InitNote; // �Ӷ���״̬��ʼ����

        public StateAnimeNote AnimeNote { get; private set; } // �Ӷ���״̬��ʼ����

        public StateJudgeAnimeNote JudgeNoteAnime; // ���ű����еĶ����������� Disappear �ķ�֧

        public StateDisappearNote DisappearNoteAnime; // ���û���оʹ�����ʧ�����߼�

        public StateDestroyNote DestroyNoteAnime; // ���л���ʧ֮��Ҫɾ��note

        private StateMachine<NoteBehaviour> JudgeMachine; // �ж�״̬��

        public StateBeforeJudgeNote BeforeJudge; // �����ж���֮ǰ

        public StateOnJudgeNote OnJudge; // ���ж�������

        public StateAfterJudgeNote AfterJudge; // �����ж�����

        public TrackBehaviour ParentTrack; // ��������һ���������ȡ transform.position ��Ϊ��������ԭ�㣨Ĭ������䵽����ϣ�

        public float JudgeTime { get; private set; } // Ԥ���ж�ʱ��

        public bool IsJudged; // �Ƿ��ж�

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
            OnJudge = new(Note, JudgeMachine);
            AfterJudge = new(Note, JudgeMachine);

            JudgeMachine.InitState(BeforeJudge);
            StateMachine.InitState(InitNote);
        }

        private void Update() // ����״̬��
        {
            JudgeMachine.CurState?.Update();
            StateMachine.CurState?.Update();
        }

        public void AnimeSwitchToJudge()
        {
            if (Judge.GetJudgeEnum(this) != Judge.NoteJudgeEnum.NotEntered)
            {
                StateMachine.SwitchState(JudgeNoteAnime);
            }
        }

        public void JudgeAction()
        {
            if (!IsJudged)
            {
                IsJudged = true;
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
    }
}
