using System.Collections.Generic;
using Anime;
using GameBehaviourManager;
using IUtils;
using NBehaviour;
using TBehaviour;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Game = GameBehaviourManager.GameBehaviour;
using Rand = UnityEngine.Random;

/// <summary>
/// 负责管理游戏核心逻辑的类
/// </summary>
namespace GameMain
{
    public class GameMain : MonoBehaviour, IDev
    {
        private static ObjectPool<GameObject> NotePool;

        private static ObjectPool<GameObject> TrackPool;

        public delegate void UpdateHandler();

        public UpdateHandler UpdateWhenKeyPressed;

        public float LastSaveLoadTimeCache; // 暂时只用来记录上一次 保存 | 读取 配置的时间，可以删除

        internal static GameSettings GameSettings;

        public System.Random Rand = new();

        public GameObject NoteInst; // 不同颜色的Note已合并为同一个游戏物件，用不同的Sprite表示

        public GameObject TrackInst;

        public bool CurFlag;

        public bool isDev;

        public bool TestFlag;

        public static int CurNoteCount;

        public static int TrackIdIterator;

        private void InitPools()
        {
            NotePool = new ObjectPool<GameObject>(
                InstantiateNote,
                (Note) =>
                {
                    Note.SetActive(true);
                },
                (Note) =>
                {
                    Note.SetActive(false);
                },
                (Note) =>
                {
                    Destroy(Note);
                },
                true,
                64,
                128
            );

            TrackPool = new ObjectPool<GameObject>(
                InstantiateTrack,
                (Track) =>
                {
                    Track.SetActive(true);
                },
                (Track) =>
                {
                    Track.SetActive(false);
                },
                (Track) =>
                {
                    Destroy(Track);
                },
                true,
                8,
                32 - 4 // 这里的 maxSize 竟然只限制池内未启用物体数量....
            );
        }

        private GameObject InstantiateTrack()
        {
            GameObject Track = Instantiate(TrackInst, transform);

            Track
                .GetComponent<TrackBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    TrackPool.Release(Track);

                    TrackIdIterator--;
                });

            // Another Stupid Line Of Code Once Lived Here.

            return Track;
        }

        private GameObject InstantiateNote()
        {
            GameObject Note = Instantiate(NoteInst, transform);

            Note.GetComponent<NoteBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    // A Stupid Line Of Code Once Lived Here, but Not Forever.

                    NotePool.Release(Note);

                    CurNoteCount--;
                });

            return Note;
        }

        private void InitGameSettings()
        {
            GameSettings = new(); // 用一个对象存储游戏设置，方便读取，存储和修改
        }

        private void Start()
        {
            InitPools();

            InitGameSettings();

            UpdateWhenKeyPressed += LoadGameSettings;

            UpdateWhenKeyPressed += SaveGameSettings;

            UpdateWhenKeyPressed += TestNote;

            UpdateWhenKeyPressed += TestTrack;
        }

        void Update()
        {
            UpdateWhenKeyPressed();
        }

        private void TestNote()
        {
            if (Input.GetKeyDown(GameSettings.KeyGameTestNote))
            {
                GetNotesDynamic();
            }
        }

        private void TestTrack()
        {
            if (Input.GetKey(GameSettings.KeyGameTestTrack))
            {
                GetTracksDynamic();
            }
        }

        private void SaveGameSettings()
        {
            if (Input.GetKeyDown(GameSettings.KeyGameSave))
            {
                LastSaveLoadTimeCache = Game.Inst.GetAbsTime();
                Game.Inst.SaveGameSettings();
            }
        }

        private void LoadGameSettings()
        {
            if (Input.GetKeyDown(GameSettings.KeyGameLoad))
            {
                LastSaveLoadTimeCache = Game.Inst.GetAbsTime();
                Game.Inst.LoadGameSettings(ref GameSettings);
            }
        }

        /// <summary>
        /// 在游戏过程中动态生成所需的 <see cref="NoteBehaviour"/> 对象，需要读取谱面文件作为需求列表。
        /// TODO : 实现读取文件和结构化存储Note信息
        /// TODO ?: 适配.mcz
        /// </summary>
        private void GetNotesDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                var Tmp = new Queue<AnimeClip>();
                var VecTmp = new Vector3(-1f, 1.5f, 0f);
                Tmp.Enqueue(
                    new AnimeClip(
                        Game.Inst.GetGameTime(),
                        Game.Inst.GetGameTime() + 0.65f,
                        VecTmp,
                        VecTmp - new Vector3(0f, 2.5f, 0f)
                    )
                );

                GetOneNote(Tmp);
            }
        }

        private void GetTracksDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                var Tmp = new Queue<AnimeClip>();
                var VecTmp = new Vector3(1f, 1.5f, 0f);
                Tmp.Enqueue(
                    new AnimeClip(
                        Game.Inst.GetGameTime(),
                        Game.Inst.GetGameTime() + 0.65f,
                        VecTmp,
                        VecTmp - new Vector3(0f, 2.5f, 0f)
                    )
                );

                GetOneTrack(Tmp);
            }
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="NoteBehaviour"/> 对象，并同时初始化它的动画队列
        /// </summary>
        private void GetOneNote(Queue<AnimeClip> AnimeQueue)
        {
            GameObject CurObj = NotePool.Get();

            CurObj.GetComponent<NoteBehaviour>().AnimeQueue = AnimeQueue;

            CurNoteCount++;
        }

        private void GetOneTrack(Queue<AnimeClip> AnimeQueue)
        {
            GameObject CurObj = TrackPool.Get();

            CurObj.GetComponent<TrackBehaviour>().AnimeQueue = AnimeQueue; // 有一个人复制粘贴代码忘了改名字，是谁呢好难猜呀

            CurObj.GetComponent<TrackBehaviour>().TrackId = TrackIdIterator;

            CurObj.GetComponent<TrackBehaviour>().PostResetLock = true;

            TrackIdIterator++;
        }

        public Vector3 RandVec()
        {
            return new Vector3(
                (float)(3f * (Random.value - 0.5)),
                (float)(2f * (Random.value - 0.5)),
                (float)(2f * (Random.value - 0.5))
            );
        }

        public Vector3 FlatRandVec()
        {
            return new Vector3(
                (float)(4f * (Random.value - 0.5)),
                (float)(2f * (Random.value - 0.5)),
                0f
            );
        }

        public Vector3 StraightRandVec(float Y)
        {
            return new Vector3((float)(3f * (Random.value - 0.5)), Y, (float)(Random.value + 1f));
        }

        public void DevLog()
        {
            Game.Inst.DevLog();
        }

        public static int GetNotePoolCountInactive()
        {
            return NotePool.CountInactive;
        }

        public static int GetCurNoteCount()
        {
            return CurNoteCount;
        }
    }
}
