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
        private StateMachine<TrackBehaviour> StateMachine; // 动画状态机

        public StateInitTrack InitTrack; // 从动画状态开始更新

        public StateAnimeTrack AnimeTrack; // 从动画状态开始更新

        public StateDisappearTrack DisappearTrack; // 消失

        public StateDestroyTrack DestroyTrack; // 摧毁该物体

        private StateMachine<TrackBehaviour> JudgeMachine; // 负责管理 Note 判定

        public StateInitJudgeTrack InitJudge; // 初始化判定逻辑，暂时只有注册 Input 判定

        public StateProcessJudgeTrack ProcessJudge; // 处理判断序列

        public StateFinishJudgeTrack FinishJudge; // 注销 Input 判定，等待游戏结束

        public List<NoteBehaviour> JudgeList; // 比队列好的一点：可指定删除元素

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

        // 这个函数塞到状态机里比较麻烦（要在状态机里注册函数委托）
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

        public TrackBehaviour Init(AnimeMachine Machine, int Number) // 在 Objectpool 中调用这个函数作为通用初始化，保证每次调用都从这里开始
        {
            TrackNumber = Number;
            AnimeMachine = Machine;

            InitStateMachine(this);

            return this;
        }
    }
}
