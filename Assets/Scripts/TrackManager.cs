using System.Collections.Generic;
using Anime;
using PooledObject;
using StateMachine;
using TrackStateMachine;

namespace TrackManager
{
    /*
     * ��ʷ�Ե�һ��
     *
     * �Ұ�Note��ص������ļ�������һ��
     *
     * ���������е� "Note" �ַ��滻���� "Track"
     *
     * ��ȫ��������
     *
     * ����֪
     */

    public class TrackBehaviour : PooledObjectBehaviour
    {
        public StateMachine<TrackBehaviour> StateMachine; // ״̬��

        public StateInitTrack InitTrack; // �Ӷ���״̬��ʼ����

        public StateAnimeTrack AnimeTrack; // �Ӷ���״̬��ʼ����

        public StateDisappearTrack DisappearTrack;

        public StateDestroyTrack DestroyTrack;

        public float JudgeTime;

        public bool isJudged = false;

        public bool isFake = false;

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

        public override void Init(AnimeMachine Machine) // �� Objectpool �е������������Ϊͨ�����֣���֤ÿ�ε��ö������￪ʼ
        {
            base.Init(Machine);
            InitStateMachine(this);
        }

        public override PooledObjectBehaviour GetBase() // ����û�ҵ�������ϣ�ֻ������ô�պ���
        {
            return base.GetBase();
        }
    }
}
