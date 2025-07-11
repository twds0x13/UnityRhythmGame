using System.Collections.Generic;
using Anime;
using GameManager;
using NoteManager;
using PooledObject;
using TrackManager;
using UnityEngine;
using Game = GameManager.GameManager;
using Pool = PooledObject.PooledObjectManager;

/// <summary>
/// 负责管理游戏核心逻辑的类
/// 包括场景切换，单例类管理，
/// </summary>
namespace GameCore
{
    public class GameController : MonoBehaviour
    {
        public static GameController Inst { get; private set; }

        public System.Action OnUpdate;

        private GameSettings _Settings;

        public bool isDeveloperMode;

        public bool isGameTesting;

        private void InitInstance()
        {
            if (Inst != null && Inst != this)
            {
                Destroy(gameObject);
                return;
            }
            Inst = this;

            DontDestroyOnLoad(gameObject);
        }

        private void InitGameSettings()
        {
            _Settings = Game.Inst.GameSettings;
        }

        private void Awake()
        {
            InitInstance();

            InitGameSettings();

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

        private void TestNote()
        {
            if (Input.GetKey(_Settings.KeyGameTestNote))
            {
                Pool.Inst.GetNotesDynamic();
            }
        }

        private void TestTrack()
        {
            if (Input.GetKeyDown(_Settings.KeyGameTestTrack))
            {
                Pool.Inst.GetTracksDynamic();
            }
        }

        private void SaveGameSettings()
        {
            if (Input.GetKeyDown(_Settings.KeyGameSave))
            {
                Game.Inst.SaveGameSettings();
            }
        }

        private void LoadGameSettings()
        {
            if (Input.GetKeyDown(_Settings.KeyGameLoad))
            {
                Game.Inst.LoadGameSettings(ref _Settings);
            }
        }

        private void TestIgnorePause()
        {
            if (Input.GetKey(KeyCode.C))
            {
                Game.Inst.LockTimeScale(2f);
            }
        }
    }
}
