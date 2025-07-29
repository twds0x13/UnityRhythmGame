using Anime;
using PooledObject;
using StateMachine;
using TrackStateMachine;
using UnityEngine;

namespace TrackNamespace
{
    public class TrackBehaviour : PooledObjectBehaviour
    {
        public StateMachine<TrackBehaviour> StateMachine; // ״̬��

        public StateInitTrack InitTrack; // �Ӷ���״̬��ʼ����

        public StateAnimeTrack AnimeTrack; // �Ӷ���״̬��ʼ����

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

        public void Init(AnimeMachine Machine, int Number) // �� Objectpool �е������������Ϊͨ�����֣���֤ÿ�ε��ö������￪ʼ
        {
            TrackNumber = Number;
            AnimeMachine = Machine;
            InitStateMachine(this);
        }
    }
}
