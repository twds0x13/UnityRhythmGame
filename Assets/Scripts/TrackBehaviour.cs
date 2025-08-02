using System.Collections.Generic;
using Anime;
using NoteNS;
using PooledObjectNS;
using StateMachine;
using TrackStateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Game = GameManagerNS.GameManager;

namespace TrackNS
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

        private List<NoteBehaviour> AllList = new(); // ����ɾ��������������ʹ��

        private List<NoteBehaviour> JudgeList = new(); // �ȶ��кõ�һ�㣺��ָ��ɾ��Ԫ��

        public BaseUIPage ParentPage; // ĸҳ��

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
            JudgeMachine.CurState?.Update();
        }

        /*
         
        ����������£�JudgeList Ӧ���ǰ��� Note �� JudgeTime �Ⱥ��������е� ������������һ�������ܳ��� Note Ҳ������

        ��Ϊ���� OnJudge ״̬�������� Note ���� Miss ���䣬�� Miss ����ļ������� JudgeTime ��ȥ NoteJudgeTime.Miss������һ��������

        �����ڸ�ֵ��ʱ�� JudgeTime �Ĵ�С˳����� JudgeList �б��ڵ�ǰ��˳��

        ���� JudgeList ��һ�������б�

        �����Ѿ�ͨ�� RegisterJudge() �� UnregisterJudge() ʵ���� JudgeList ��δ�ж� Note ���Զ��Ƴ�

        ��֪ �û�ÿ����һ��������ֻ�����һ������������ܺ���ͬ���������

        ���� ֻ��Ҫ���б�ǿյ������ ÿ��ȡ���б��еĵ�һ�� Note Ȼ�󴥷��ж��¼�
        
        ���Ǿ������һ������Ҫ�����ж��б���ж�������
        
        */

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
                    JudgeList[0].OnJudge();
                }
            }
        }

        public override void OnClosePage()
        {
            // ������...... �Ҽ���һ�� AllList Ȼ�����˰������ JudgeList.Count �ĳ� AllList.Count

            foreach (NoteBehaviour Note in AllList)
            {
                Note.OnClosePage();
            }

            JudgeMachine.SwitchState(FinishJudge);
            StateMachine.SwitchState(DisappearTrack);
        }

        public TrackBehaviour Init(BaseUIPage Page, AnimeMachine Machine, int Number) // �� Objectpool �е������������Ϊͨ�ó�ʼ������֤ÿ�ε��ö������￪ʼ
        {
            ParentPage = Page;
            TrackNumber = Number;
            AnimeMachine = Machine;

            InitStateMachine(this);

            return this;
        }

        public void Register(NoteBehaviour Note)
        {
            AllList.Add(Note);
        }

        public void Unregister(NoteBehaviour Note)
        {
            AllList.Remove(Note);
        }

        public void RegisterJudge(NoteBehaviour Note)
        {
            JudgeList.Add(Note);
        }

        public void UnregisterJudge(NoteBehaviour Note)
        {
            JudgeList.Remove(Note);
        }
    }
}
