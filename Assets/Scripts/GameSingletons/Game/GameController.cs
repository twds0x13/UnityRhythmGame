using System;
using Cysharp.Threading.Tasks;
using Singleton;
using UnityEngine;
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

        public Action ActionOnUpdate;

        protected override void SingletonAwake()
        {
            // 在初始化时移除旧的 OnUpdate 订阅
            ActionOnUpdate -= OnUpdate;

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

        public void StartGame()
        {
            OnStartGame().Forget();
        }

        public void ExitGame()
        {
            OnExitGame();
        }

        private async UniTaskVoid OnStartGame()
        {
            await UniTask.WaitForSeconds(1.5f);

            ActionOnUpdate += OnUpdate;
        }

        private void OnExitGame()
        {
            ActionOnUpdate -= OnUpdate;
        }

        private void Update()
        {
            ActionOnUpdate?.Invoke();
        }

        /// <summary>
        /// 核心游戏循环逻辑。
        /// </summary>
        private void OnUpdate() { } // 哈 这里什么都不需要了

        public void RebindInput(InputActionReference Ref)
        {
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
            {
                Pool.Inst.GetNotesDynamic(Game.Inst.GetGameTime(), 1f, 1, 1f);
            }
        }

        public void GetTrack(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed && Pool.Inst.TrackUIDIterator == 0)
                Pool.Inst.GetTracksDynamic();
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
