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
        // ��ʵ֤�������弸����������״̬����� Dictionary Ȼ���� SwitchTo(Enum State) ���㡣

        public StateMachine<NoteBehaviour> StateMachine; // ״̬��

        public StateInitNote InitNote; // �Ӷ���״̬��ʼ����

        public StateAnimeNote AnimeNote; // �Ӷ���״̬��ʼ����

        public StateScoreNote ScoreNote; // ������оͲ��ŵ÷ֶ�������Ч��

        public StateDisappearNote DisappearNote; // ���û���оʹ�����ʧ�����߼�

        public StateDestroyNote DestroyNote; // ���л���ʧ֮��Ҫɾ��note

        public TrackBehaviour ParentTrack; // ��������һ���������ȡ transform.position ��Ϊ����ƫ������Ĭ������䵽����ϣ�

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

        public void Init(AnimeMachine Machine, TrackBehaviour Track) // �� Objectpool �е������������Ϊͨ�����֣���֤ÿ�ε��ö������￪ʼ
        {
            ParentTrack = Track;
            AnimeMachine = Machine;
            InitStateMachine(this);
        }
    }
}
