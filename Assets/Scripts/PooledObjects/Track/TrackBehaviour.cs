using System.Collections.Generic;
using System.Linq;
using Anime;
using AudioNS;
using AudioRegistry;
using PageNS;
using PooledObjectNS;
using StateMachine;
using TrackStateMachine;
using Audio = AudioNS.AudioManager;

namespace TrackNS
{
    public class TrackBehaviour : PooledObjectBehaviour
    {
        private LinearStateMachine<TrackBehaviour> StateMachine; // 动画状态机

        public StateInitTrack InitTrack; // 从动画状态开始更新

        public StateAnimeTrack AnimeTrack; // 从动画状态开始更新

        public StateDisappearTrack DisappearTrack; // 消失

        public StateDestroyTrack DestroyTrack; // 摧毁该物体

        private LinearStateMachine<TrackBehaviour> JudgeMachine; // 负责管理 Note 判定

        public StateInitJudgeTrack InitJudge; // 初始化判定逻辑，暂时只有注册 Input 判定

        public StateProcessJudgeTrack ProcessJudge; // 处理判断序列

        public StateFinishJudgeTrack FinishJudge; // 注销 Input 判定，等待游戏结束

        private List<IChartObject> AllList { get; } = new(); // 遍历删除或其他操作才使用

        private readonly List<IChartObject> JudgeList = new(); // 比队列好的一点：可指定删除元素

        public BaseUIPage ParentPage; // 母页面

        public ITrackInputProvider InputProvider;

        public int TrackNumber { get; private set; }

        private void InitStateMachine(TrackBehaviour Track)
        {
            StateMachine = new();

            InitTrack = new(Track, StateMachine);
            AnimeTrack = new(Track, StateMachine);
            DisappearTrack = new(Track, StateMachine);
            DestroyTrack = new(Track, StateMachine);

            // var AnimeList = new List<IState<TrackBehaviour>> { InitTrack };

            JudgeMachine = new();

            InitJudge = new(Track, JudgeMachine);
            ProcessJudge = new(Track, JudgeMachine);
            FinishJudge = new(Track, JudgeMachine);

            // var JudgeList = new List<IState<TrackBehaviour>> { InitJudge };

            JudgeMachine.InitState(InitJudge);
            StateMachine.InitState(InitTrack);
        }

        private void Update()
        {
            JudgeMachine.CurState?.Update();
            StateMachine.CurState?.Update();
        }

        /*
         
        在正常情况下，JudgeList 应该是按照 Note 的 JudgeTime 先后有序排列的 就算你先生成一个动画很长的 Note 也是这样

        因为进入 OnJudge 状态的条件是 Note 进入 Miss 区间，而 Miss 区间的计算结果是 JudgeTime 减去 NoteJudgeTime.Miss（它是一个常量）

        所以在赋值的时候 JudgeTime 的大小顺序就是 JudgeList 列表内的前后顺序

        所以 JudgeList 是一个有序列表

        我们已经通过 RegisterJudge() 和 UnregisterJudge() 实现了 JudgeList 中未判定 Note 的自动移除

        已知 用户每按下一个按键都只想击打一个音符，不想管后面同轨道的音符

        所以 只需要在列表非空的情况下 每次取出列表中的第一个 Note 然后触发判定事件
        
        我们就完成了一个不需要遍历判定列表的判定管理器
        
        */

        public void OnPressed()
        {
            if (JudgeList.Count > 0 && JudgeList[0] is not null)
            {
                JudgeList[0].OnPress();

                // 这里处理的是 key 音，也就是成功击打才会触发的音效。

                switch (TrackNumber)
                {
                    case 0:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track0);
                        break;
                    case 1:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track1);
                        break;
                    case 2:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track2);
                        break;
                    case 3:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track3);
                        break;
                }
            }
        }

        public void OnReleased()
        {
            if (JudgeList.Count > 0 && JudgeList[0] is not null)
            {
                JudgeList[0].OnRelease();

                switch (TrackNumber)
                {
                    case 0:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track0);
                        break;
                    case 1:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track1);
                        break;
                    case 2:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track2);
                        break;
                    case 3:
                        Audio.Inst.LoadAudioClip(SFX.Key3, Source.Track3);
                        break;
                }
            }
        }

        public override void OnClosePage()
        {
            foreach (PooledObjectBehaviour Object in AllList.Cast<PooledObjectBehaviour>())
            {
                Object.OnClosePage();
            }

            JudgeMachine.SwitchState(FinishJudge);
            StateMachine.SwitchState(DisappearTrack);
        }

        public TrackBehaviour Init(
            BaseUIPage page,
            AnimeMachine machine,
            ITrackInputProvider input,
            int Number
        ) // 在 Objectpool 中调用这个函数作为通用初始化，保证每次调用都从这里开始
        {
            ParentPage = page;
            TrackNumber = Number;
            AnimeMachine = machine;

            InputProvider = input;

            InitStateMachine(this);

            return this;
        }

        public void OnSwitchProvider(ITrackInputProvider input)
        {
            InputProvider = input;

            if (!InputProvider.IsRegistered(TrackNumber))
            {
                InputProvider.Register(TrackNumber, OnPressed, OnReleased);
            }
        }

        public void ResetTrack()
        {
            ParentPage = null;
            TrackNumber = -1;
            AnimeMachine = null;

            AllList.Clear();
            JudgeList.Clear();
        }

        public void Register(IChartObject Note)
        {
            AllList.Add(Note);
        }

        public void Unregister(IChartObject Note)
        {
            AllList.Remove(Note);
        }

        public void RegisterJudge(IChartObject Note)
        {
            JudgeList.Add(Note);
        }

        public void UnregisterJudge(IChartObject Note)
        {
            JudgeList.Remove(Note);
        }
    }
}
