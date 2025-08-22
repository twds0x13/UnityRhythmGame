using System;
using Singleton;
using UnityEngine;
using UnityEngine.InputSystem;
using Game = GameManagerNS.GameManager;
using Pool = PooledObjectNS.PooledObjectManager;

/// <summary>
/// 负责管理游戏核心逻辑的类
/// 包括场景切换，单例类管理等，用游戏全局状态机驱动
/// </summary>
namespace GameCore
{
    [RequireComponent(typeof(PlayerInput))]
    #region GameController
    public class GameController : Singleton<GameController>
    {
        public PlayerInput UserInput;

        // 测试用临时变量

        bool _flag;

        System.Random Rand = new();

        protected override void SingletonAwake() { }

        // 仅供测试使用
        #region WUT R U DOEN?
        void Update()
        {
            var Time = Game.Inst.GetGameTime();

            var Vertical = 1.00f;

            if (Math.Ceiling(Time * 10f) % 2 == 0 && _flag)
            {
                var FirstNum = Rand.Next(0, 4);

                Pool.Inst.GetNotesDynamic(Time, Vertical, FirstNum, 0.8f);

                if (Pool.Inst.TrackUIDIterator > 1) // 假装在打大叠
                {
                    var SecondNum = Rand.Next(0, 4);

                    while (SecondNum == FirstNum)
                    {
                        SecondNum = Rand.Next(0, 4);
                    }

                    Pool.Inst.GetNotesDynamic(Time, Vertical, SecondNum, 0.8f);
                }

                _flag = false;
            }

            if (Math.Ceiling(Time * 10f) % 2 == 1)
            {
                _flag = true;
            }
        }
        #endregion

        public void RebindInput(InputActionReference Ref)
        {
            // Debug.Log("Iz Thiz Jvav?");
            UserInput.SwitchCurrentActionMap("UserRebinding");
            Ref.action.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .WithCancelingThrough("<keyboard>/escape")
                .OnMatchWaitForAnother(0.05f)
                .OnComplete(Operation => SwitchToNormal(Operation))
                .OnCancel(Operation => SwitchToNormal(Operation))
                .Start();
        }

        private void SwitchToNormal(InputActionRebindingExtensions.RebindingOperation Operation)
        {
            Operation.Dispose();
            UserInput.SwitchCurrentActionMap("UserNormal");
        }

        #region GameTests

        public void GetNote(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
                Pool.Inst.GetNotesDynamic(Game.Inst.GetGameTime(), 1f, Rand.Next(0, 4), 1f);
        }

        public void GetTrack(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed && Pool.Inst.TrackUIDIterator == 0)
                Pool.Inst.GetTracksDynamic();
        }

        public void SaveGameSettings(InputAction.CallbackContext Ctx)
        {
            Game.Inst.SaveGameSettings();
        }

        public void LoadGameSettings(InputAction.CallbackContext Ctx)
        {
            Game.Inst.LoadGameSettings(ref Game.Inst.Settings);
        }

        public void TestIgnorePause(InputAction.CallbackContext Ctx)
        {
            //Game.Inst.LockTimeScale(2f);
        }

        public void PauseResumeGame(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
                Game.Inst.PauseResumeGame();
        }

        #endregion

        public float RandFloat(float Min, float Max)
        {
            return Min + (float)Rand.NextDouble() * (Max - Min);
        }
    }

    #endregion
}
