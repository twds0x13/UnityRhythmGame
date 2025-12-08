using System;
using Cysharp.Threading.Tasks;
using Singleton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Game = GameManagerNS.GameManager;
using Input = InputProviderManager;
using Pool = PooledObjectNS.PooledObjectManager;

/// <summary>
/// 负责管理游戏核心逻辑的类
/// </summary>
namespace GameCore
{
    [RequireComponent(typeof(PlayerInput))]
    #region GameController
    public class GameController : Singleton<GameController>
    {
        public PlayerInput UserInput;

        protected override void SingletonAwake()
        {
            Input.Inst.AddProvider<AutoPlayTrackInputProvider>(new(ChartManager.Inst));

            Input.Inst.AddProvider<UnityTrackInputProvider>(new(UserInput.actions));

            Input.Inst.RegisterAllToGameStart();

            Input.Inst.SwitchToProvider<UnityTrackInputProvider>();

            Pool.Inst.TrackInputProvider = Input.Inst.GetCurrentProvider();
        }

        public void ToggleAutoPlay()
        {
            switch (Input.Inst.GetCurrentProvider())
            {
                case UnityTrackInputProvider:

                    Input.Inst.SwitchToProvider<AutoPlayTrackInputProvider>();

                    Pool.Inst.TrackInputProvider = Input.Inst.GetCurrentProvider();

                    break;

                case AutoPlayTrackInputProvider:

                    Input.Inst.SwitchToProvider<UnityTrackInputProvider>();

                    Pool.Inst.TrackInputProvider = Input.Inst.GetCurrentProvider();

                    break;
            }
        }

        #region GameTests

        public void GetNote(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
            {
                Pool.Inst.GetNotesDynamic(Game.Inst.GetGameTime(), 1f, 1, 1f);
            }
        }

        public void GetTrack(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed && Pool.Inst.TrackUIDIterator == 0) { }
            // Pool.Inst.GetTracksDynamic();
        }

        public void PauseResumeGame(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
                Game.Inst.PauseResumeGame();
        }

        #endregion

        protected override void SingletonDestroy()
        {
            Input.Inst.UnregisterAllFromGameStart();
        }
    }

    #endregion
}
