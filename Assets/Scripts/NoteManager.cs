using Anime;
using NoteStateMachine;
using PooledObject;
using StateMachine;
using TrackManager;

namespace NoteManager
{
    public class NoteBehaviour : PooledObjectBehaviour
    {
        public StateMachine<NoteBehaviour> StateMachine; // ״̬��

        // ��ʵ֤�������弸����������״̬����� Dictionary Ȼ���� SwitchTo(Enum State) �л�״̬���㡣

        public StateInitNote InitNote; // �Ӷ���״̬��ʼ����

        public StateAnimeNote AnimeNote; // �Ӷ���״̬��ʼ����

        public StateScoreNote ScoreNote; // ������оͲ��ŵ÷ֶ�������Ч��

        public StateDisappearNote DisappearNote; // ���û���оʹ�����ʧ�����߼�

        public StateDestroyNote DestroyNote; // ���л���ʧ֮��Ҫɾ��note

        public TrackBehaviour ParentTrack; // ��������һ���������ȡ transform.position ��Ϊ����ƫ������Ĭ������䵽����ϣ�

        public int UID;

        public float JudgeTime;

        public bool isJudged = false;

        public bool isFake = false;

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
