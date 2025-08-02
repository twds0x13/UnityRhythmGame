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
        private StateMachine<TrackBehaviour> StateMachine; // 动画状态机

        public StateInitTrack InitTrack; // 从动画状态开始更新

        public StateAnimeTrack AnimeTrack; // 从动画状态开始更新

        public StateDisappearTrack DisappearTrack; // 消失

        public StateDestroyTrack DestroyTrack; // 摧毁该物体

        private StateMachine<TrackBehaviour> JudgeMachine; // 负责管理 Note 判定

        public StateInitJudgeTrack InitJudge; // 初始化判定逻辑，暂时只有注册 Input 判定

        public StateProcessJudgeTrack ProcessJudge; // 处理判断序列

        public StateFinishJudgeTrack FinishJudge; // 注销 Input 判定，等待游戏结束

        private List<NoteBehaviour> AllList = new(); // 遍历删除或其他操作才使用

        private List<NoteBehaviour> JudgeList = new(); // 比队列好的一点：可指定删除元素

        public BaseUIPage ParentPage; // 母页面

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
         
        在正常情况下，JudgeList 应该是按照 Note 的 JudgeTime 先后有序排列的 就算你先生成一个动画很长的 Note 也是这样

        因为进入 OnJudge 状态的条件是 Note 进入 Miss 区间，而 Miss 区间的计算结果是 JudgeTime 减去 NoteJudgeTime.Miss（它是一个常量）

        所以在赋值的时候 JudgeTime 的大小顺序就是 JudgeList 列表内的前后顺序

        所以 JudgeList 是一个有序列表

        我们已经通过 RegisterJudge() 和 UnregisterJudge() 实现了 JudgeList 中未判定 Note 的自动移除

        已知 用户每按下一个按键都只想击打一个音符，不想管后面同轨道的音符

        所以 只需要在列表非空的情况下 每次取出列表中的第一个 Note 然后触发判定事件
        
        我们就完成了一个不需要遍历判定列表的判定管理器
        
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
            // 好弱智...... 我加了一个 AllList 然后忘了把这里的 JudgeList.Count 改成 AllList.Count

            foreach (NoteBehaviour Note in AllList)
            {
                Note.OnClosePage();
            }

            JudgeMachine.SwitchState(FinishJudge);
            StateMachine.SwitchState(DisappearTrack);
        }

        public TrackBehaviour Init(BaseUIPage Page, AnimeMachine Machine, int Number) // 在 Objectpool 中调用这个函数作为通用初始化，保证每次调用都从这里开始
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
