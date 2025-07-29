using System;
using Singleton;
using UnityEngine;
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
        public Action OnUpdate;

        protected override void SingletonAwake()
        {
            OnUpdate += LoadGameSettings;

            OnUpdate += SaveGameSettings;

            OnUpdate += TestNote;

            OnUpdate += TestTrack;

            OnUpdate += TestIgnorePause;
        }

        void Update()
        {
            OnUpdate();
        }

        #region GameTests

        private void TestNote()
        {
            if (Input.GetKeyDown(Game.Inst.Settings.KeyGameTestNote))
            {
                Pool.Inst.GetNotesDynamic();
            }
        }

        private void TestTrack()
        {
            if (Input.GetKeyDown(Game.Inst.Settings.KeyGameTestTrack))
            {
                Pool.Inst.GetTracksDynamic();
            }
        }

        private void SaveGameSettings()
        {
            if (Input.GetKeyDown(Game.Inst.Settings.KeyGameSave))
            {
                Game.Inst.SaveGameSettings();
            }
        }

        private void LoadGameSettings()
        {
            if (Input.GetKeyDown(Game.Inst.Settings.KeyGameLoad))
            {
                Game.Inst.LoadGameSettings(ref Game.Inst.Settings);
            }
        }

        private void TestIgnorePause()
        {
            if (Input.GetKey(KeyCode.C))
            {
                Game.Inst.LockTimeScale(2f);
            }
        }

        #endregion
    }
    #endregion
}
