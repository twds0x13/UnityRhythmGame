using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Singleton;
using UnityEngine;
using UnityEngine.InputSystem;
using Game = GameManagerNS.GameManager;
using Parser = ChartParser.ChartParser;
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

        private Parser chartParser;

        protected override void SingletonAwake()
        {
            // 在初始化时移除旧的 OnUpdate 订阅
            ActionOnUpdate -= OnUpdate;

            LoadChart();
        }

        public async void LoadChart()
        {
            chartParser = new Parser(Application.persistentDataPath);

            var collection = await chartParser.ScanAsync();
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
        private void OnUpdate()
        {
            // 移除所有 ChartReader 相关逻辑
            // 可以在这里添加其他游戏逻辑
        }

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
            // Game.Inst.LockTimeScale(2f);
        }

        public void PauseResumeGame(InputAction.CallbackContext Ctx)
        {
            if (Ctx.performed)
                Game.Inst.PauseResumeGame();
        }

        #endregion

        protected override void SingletonDestroy()
        {
            DOTween.KillAll();
            DOTween.Clear(true);
        }
    }

    #endregion
}
