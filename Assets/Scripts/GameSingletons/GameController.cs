using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Singleton;
using UnityEngine;
using UnityEngine.InputSystem;
using Game = GameManagerNS.GameManager;
using Pool = PooledObjectNS.PooledObjectManager;

/// <summary>
/// 负责管理游戏核心逻辑的类
/// 已被架空，暂时用来处理和游戏输入有关的东西和游戏测试
/// </summary>
namespace GameCore
{
    [RequireComponent(typeof(PlayerInput))]
    #region GameController
    public class GameController : Singleton<GameController>
    {
        public PlayerInput UserInput;

        private CancellationTokenSource _cancellationTokenSource;

        private int[] availableTracks = { 0, 1, 2, 3 };

        private async UniTaskVoid StartNoteGeneration()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // 130 BPM下的8分音符间隔
            float interval = 60f / 130f / 2f;

            while (!token.IsCancellationRequested)
            {
                if (!Game.Inst.IsGamePaused())
                {
                    float currentTime = Game.Inst.GetGameTime();

                    // 随机选择两个不同的轨道
                    int[] randomTracks = GetTwoRandomTracks();

                    // 生成两个不同轨道的音符
                    GenerateSimpleNote(currentTime, randomTracks[0]);
                    GenerateSimpleNote(currentTime, randomTracks[1]);
                }

                await UniTask.Delay((int)(interval * 1000), cancellationToken: token);
            }
        }

        private int[] GetTwoRandomTracks()
        {
            // 随机打乱轨道数组并取前两个
            return availableTracks.OrderBy(x => Random.Range(0, 100)).Take(2).ToArray();
        }

        private void GenerateSimpleNote(float startTime, int trackNum)
        {
            // 使用您原有的GetNotesDynamic方法
            Pool.Inst.GetNotesDynamic(startTime, 1f, trackNum, 1f);
        }

        protected override void SingletonAwake()
        {
            UniTask.Void(StartNoteGeneration);
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }

    #endregion
}
