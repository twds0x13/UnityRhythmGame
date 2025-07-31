using System.Collections.Generic;
using Anime;
using NoteNamespace;
using PooledObject;
using StateMachine;
using TrackStateMachine;
using UnityEngine.InputSystem;
using Game = GameManager.GameManager;

namespace TrackNamespace
{
    public class TrackBehaviour : PooledObjectBehaviour
    {
        private StateMachine<TrackBehaviour> StateMachine; // ����״̬��

        public StateInitTrack InitTrack; // �Ӷ���״̬��ʼ����

        public StateAnimeTrack AnimeTrack; // �Ӷ���״̬��ʼ����

        public StateDisappearTrack DisappearTrack; // ��ʧ

        public StateDestroyTrack DestroyTrack; // �ݻٸ�����

        private StateMachine<TrackBehaviour> JudgeMachine; // ������� Note �ж�

        public StateInitJudgeTrack InitJudge; // ��ʼ���ж��߼�����ʱֻ��ע�� Input �ж�

        public StateProcessJudgeTrack ProcessJudge; // �����ж�����

        public StateFinishJudgeTrack FinishJudge; // ע�� Input �ж����ȴ���Ϸ����

        public List<NoteBehaviour> JudgeList; // �ȶ��кõ�һ�㣺��ָ��ɾ��Ԫ��

        public int TrackNumber { get; private set; }

        private void InitStateMachine(TrackBehaviour Track)
        {
            StateMachine = new();
            InitTrack = new(Track, StateMachine);
            AnimeTrack = new(Track, StateMachine);
            DisappearTrack = new(Track, StateMachine);
            DestroyTrack = new(Track, StateMachine);

            JudgeMachine = new();

            InitJudge = new(Track, JudgeMachine);
            ProcessJudge = new(Track, JudgeMachine);
            FinishJudge = new(Track, JudgeMachine);

            JudgeMachine.InitState(InitJudge);
            StateMachine.InitState(InitTrack);
        }

        private void Update()
        {
            StateMachine.CurState?.Update();
        }

        // �����������״̬����Ƚ��鷳��Ҫ��״̬����ע�ắ��ί�У�
        public void JudgeNote(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed && !Game.Inst.IsGamePaused())
            {
                var path = Ctx.action.bindings[0].effectivePath;
                var text = InputControlPath.ToHumanReadableString(
                    path,
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );

                if (JudgeList.Count > 0)
                {
                    JudgeList[0].JudgeAction();
                }
            }
        }

        public TrackBehaviour Init(AnimeMachine Machine, int Number) // �� Objectpool �е������������Ϊͨ�ó�ʼ������֤ÿ�ε��ö������￪ʼ
        {
            TrackNumber = Number;
            AnimeMachine = Machine;

            InitStateMachine(this);

            return this;
        }
    }
}
