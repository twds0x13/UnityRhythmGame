using System;
using Singleton;
using UnityEngine;
using UnityEngine.InputSystem;
using Game = GameManager.GameManager;
using Pool = PooledObject.PooledObjectManager;

/// <summary>
/// 负责管理游戏核心逻辑的类
/// 包括场景切换，单例类管理等，用游戏全局状态机驱动
/// </summary>
namespace GameCore
{
    #region GameController
    public class GameController : Singleton<GameController>
    {
        [SerializeField]
        public PlayerInput NewInput;

        [SerializeField]
        InputActionReference[] InputReferences;

        float test;

        bool flag;

        float _score;
        public float Score
        {
            get { return _score; }
            set
            {
                _score = value;

                Debug.Log("Score : " + value.ToString());
            }
        }

        System.Random Rand = new();

        protected override void SingletonAwake()
        {
            NewInput = GetComponent<PlayerInput>();
        }

        void Update()
        {
            test = Game.Inst.GetGameTime();

            if (Math.Ceiling(test * 16f) % 2 == 0 && flag)
            {
                Pool.Inst.GetNotesDynamic(Rand.Next(0, 4), 0.75f);
                flag = false;
            }

            if (Math.Ceiling(test * 16f) % 2 == 1)
            {
                flag = true;
            }
        }

        public void RebindInput(InputActionReference Ref)
        {
            Debug.Log("Jvav?");
            NewInput.SwitchCurrentActionMap("UserRebinding");
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
            NewInput.SwitchCurrentActionMap("UserNormal");
        }

        #region GameTests

        public void GetNote(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
                Pool.Inst.GetNotesDynamic(Rand.Next(0, 4), 0.75f);
        }

        public void GetTrack(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed && Pool.Inst.TrackUIDIterator < 4)
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
            Game.Inst.LockTimeScale(2f);
        }

        public void PauseResumeGame(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
                Game.Inst.PauseResumeGame();
        }

        #endregion
    }

    #endregion
}
