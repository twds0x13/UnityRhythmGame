using System;
using System.Collections.Generic;
using Anime;
using Cysharp.Threading.Tasks;
using HoldNS;
using JudgeNS;
using NoteNS;
using PageNS;
using Singleton;
using TrackNS;
using UnityEngine;
using UnityEngine.Pool;
using Game = GameManagerNS.GameManager;
using Input = InputProviderManager;
using Page = UIManagerNS.PageManager;
using Random = UnityEngine.Random;

namespace PooledObjectNS
{
    /// <summary>
    /// 在游戏过程中动态生成所需的 <see cref="PooledObjectBehaviour"/> 对象，需要读取谱面文件作为需求列表。
    /// TODO : 实现读取文件和结构化存储Note信息
    /// TODO ?: 适配 .mcz 文件读取
    /// </summary>
    public class PooledObjectManager : Singleton<PooledObjectManager>
    {
        [SerializeField]
        ObjectPool<GameObject> NotePool;

        [SerializeField]
        ObjectPool<GameObject> HoldPool;

        [SerializeField]
        ObjectPool<GameObject> TrackPool;

        [SerializeField]
        ObjectPool<GameObject> JudgePool;

        [SerializeField]
        Dictionary<int, TrackBehaviour> ActiveTracks;

        [SerializeField]
        GameObject NotePrefab;

        [SerializeField]
        GameObject HoldPrefab;

        [SerializeField]
        GameObject TrackPrefab;

        [SerializeField]
        GameObject JudgeDisplayerPrefab;

        public int NoteUIDIterator { get; private set; } = 0; // 你应该没同时用到21亿个 Note, 对吧？

        public int HoldUIDIterator { get; private set; } = 0; // 你应该没同时用到21亿个 Track, 对吧？

        public int TrackUIDIterator { get; private set; } = 0; // 这两个计数器不会降低，只会递增

        public List<NoteBehaviour> ActiveNoteList;

        public List<HoldBehaviour> ActiveHoldList;

        public Action<IVertical> VerticalModifier;

        public ITrackInputProvider TrackInputProvider;

        protected override void SingletonAwake()
        {
            InitTrackDict();
            InitPools();

            DeleyedAwake().Forget();
        }

        protected async UniTaskVoid DeleyedAwake()
        {
            await UniTask.Yield();

            Input.Inst.OnInputProviderChange += OnProviderChange;
        }

        private void Update()
        {
            foreach (var note in ActiveNoteList)
            {
                VerticalModifier?.Invoke(note);
            }

            foreach (var hold in ActiveHoldList)
            {
                VerticalModifier?.Invoke(hold);

                VerticalModifier?.Invoke(hold.TailAnimator);
            }
        }

        private void OnProviderChange()
        {
            foreach (var track in ActiveTracks.Values)
            {
                track.OnSwitchProvider(TrackInputProvider);
            }
        }

        public void InitTrackDict()
        {
            ActiveTracks = new();
        }

        public void InitPools()
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

                    var Behaviour = Note.GetComponent<NoteBehaviour>();

                    Behaviour.ResetNote(); // 保险起见

                    ActiveNoteList.Remove(Behaviour);
                },
                (Note) =>
                {
                    DestroyImmediate(Note);
                },
                true,
                128,
                512 - 128 // 这里的 maxSize 只限制池内未启用物体数量....
            );

            HoldPool = new ObjectPool<GameObject>(
                InstantiateHold,
                (Hold) =>
                {
                    Hold.SetActive(true);
                },
                (Hold) =>
                {
                    Hold.SetActive(false);

                    var Behaviour = Hold.GetComponent<HoldBehaviour>();

                    Behaviour.ResetHold(); // 保险起见

                    ActiveHoldList.Remove(Behaviour);
                },
                (Hold) =>
                {
                    DestroyImmediate(Hold);
                },
                true,
                128,
                512 - 128
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

                    Track.GetComponent<TrackBehaviour>().ResetTrack();
                },
                (Track) =>
                {
                    DestroyImmediate(Track);
                },
                true,
                4,
                32 - 4
            );

            JudgePool = new ObjectPool<GameObject>(
                InstantiateJudge,
                (judge) =>
                {
                    judge.SetActive(true);
                },
                (judge) =>
                {
                    judge.SetActive(false);
                },
                (judge) =>
                {
                    DestroyImmediate(judge);
                },
                true,
                4,
                32 - 4
            );
        }

        private GameObject InstantiateTrack()
        {
            GameObject Track = Instantiate(TrackPrefab, transform);

            Track.transform.position = new Vector3(0f, 20f, 0f);

            Track
                .GetComponent<PooledObjectBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    TrackPool.Release(Track);
                });

            return Track;
        }

        private GameObject InstantiateHold()
        {
            GameObject Hold = Instantiate(HoldPrefab, transform);

            Hold.transform.position = new Vector3(0f, 20f, 0f);

            Hold.GetComponent<PooledObjectBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    HoldPool.Release(Hold);
                });

            return Hold;
        }

        private GameObject InstantiateNote()
        {
            GameObject Note = Instantiate(NotePrefab, transform);

            Note.transform.position = new Vector3(0f, 20f, 0f);

            Note.GetComponent<PooledObjectBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    NotePool.Release(Note);
                });

            return Note;
        }

        private GameObject InstantiateJudge()
        {
            GameObject Judge = Instantiate(JudgeDisplayerPrefab, transform);

            Judge.transform.position = new Vector3(0f, 20f, 0f);

            Judge
                .GetComponent<JudgeBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    JudgePool.Release(Judge);
                });

            return Judge;
        }

        /// <summary>
        /// 获取一个 <see cref="NoteBehaviour"/> 对象，保证在 <paramref name="generateTime"/> + <paramref name="fallDuration"/> 时间点落在对应轨道上
        /// </summary>
        /// <param name="generateTime"></param>
        /// <param name="Vertical"></param>
        /// <param name="TrackNum"></param>
        /// <param name="fallDuration"></param>
        public void GetNotesDynamic(
            float generateTime,
            float Vertical,
            int TrackNum,
            float fallDuration
        )
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();

                Rect Rect = ResizeDetector.Inst.Rect.rect;

                Tmp.Enqueue(
                    new AnimeClip(
                        generateTime,
                        generateTime + fallDuration,
                        new Vector3(0f, Rect.height * 0.85f * Vertical, 0f),
                        new Vector3(0f, 0f, 0f)
                    )
                );

                AnimeMachine Machine = new(Tmp)
                {
                    HasJudgeAnime = true, // 可以像这样修改动画机的属性
                };

                if (ActiveTracks.TryGetValue(TrackNum, out TrackBehaviour Object))
                {
                    GetOneNote(Machine, Object, generateTime + fallDuration);
                }
            }
        }

        /// <summary>
        /// 获取一个 <see cref="HoldBehaviour"/> 对象，保证在 <paramref name="generateTime"/> + <paramref name="fallDuration"/> 时间点落在对应轨道上，并在 <paramref name="destinationTime"/> + <paramref name="fallDuration"/> 时间点结束
        /// </summary>
        /// <param name="generateTime"></param>
        /// <param name="destinationTime"></param>
        /// <param name="vertical"></param>
        /// <param name="trackNum"></param>
        /// <param name="fallDuration"></param>
        public void GetHoldsDynamic(
            float generateTime,
            float destinationTime,
            float vertical,
            int trackNum,
            float fallDuration
        )
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();

                Rect Rect = ResizeDetector.Inst.Rect.rect;

                Tmp.Enqueue(
                    new AnimeClip(
                        generateTime,
                        generateTime + fallDuration,
                        new Vector3(0f, Rect.height * 0.85f * vertical, 0f),
                        new Vector3(0f, 0f, 0f)
                    )
                );

                AnimeMachine Machine = new(Tmp)
                {
                    HasJudgeAnime = true, // 切换判定动画开关
                };

                LogManager.Log(
                    $"{generateTime},{destinationTime},{vertical},{trackNum},{fallDuration}",
                    nameof(PooledObjectManager),
                    false
                );

                if (ActiveTracks.TryGetValue(trackNum, out TrackBehaviour Object))
                {
                    GetOneHold(
                        Machine,
                        Object,
                        generateTime + fallDuration,
                        destinationTime + fallDuration
                    );
                }
            }
        }

        public void GetJudgeDynamic(JudgeEnum judgeEnum)
        {
            GetOneJudge(Page.Inst.CurPage, judgeEnum);
        }

        public void StartGame()
        {
            TrackInputProvider.Enable();

            GetTracksDynamic();
        }

        public void StartPreviewSettings()
        {
            TrackInputProvider.Enable();

            GetPreviewTrack();
        }

        /// <summary>
        /// 按照默认情况同时生成四个 <see cref="TrackBehaviour"/> 对象。
        /// </summary>
        private void GetTracksDynamic()
        {
            Queue<AnimeClip> Tmp;
            Vector3 VecTmp;
            Rect Rect = ResizeDetector.Inst.Rect.rect;
            Rect.width = 1080f;

            for (int j = 0; j < 4; j++)
            {
                Tmp = new();

                VecTmp = new Vector3(
                    -0.225f * Rect.width + 0.15f * Rect.width * TrackUIDIterator,
                    -0.28f * Rect.height,
                    0f
                );

                /*
                for (int i = 0; i < 50; i++)
                {
                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + 10 * i,
                            Game.Inst.GetGameTime() + 5f + 10 * i,
                            VecTmp,
                            VecTmp - new Vector3(0f, 0.1f * Rect.height, 0f)
                        )
                    );

                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + 5f + 10 * i,
                            Game.Inst.GetGameTime() + 10f + 10 * i,
                            VecTmp - new Vector3(0f, 0.1f * Rect.height, 0f),
                            VecTmp
                        )
                    );
                }
                */

                Tmp.Enqueue(
                    new AnimeClip(
                        Game.Inst.GetGameTime(),
                        Game.Inst.GetGameTime() + 114514f,
                        VecTmp,
                        VecTmp
                    )
                );

                AnimeMachine Machine = new(Tmp);

                GetOneTrack(Page.Inst.CurPage, Machine);
            }
        }

        /// <summary>
        /// 在 Game Settings 页面中预览 Note下落速度、Note下落时间、判定显示位置（ Perfect, Great, Miss 等 ）需要的轨道
        /// </summary>
        private void GetPreviewTrack()
        {
            Queue<AnimeClip> Tmp;
            Vector3 VecTmp;
            Rect Rect = ResizeDetector.Inst.Rect.rect;
            Rect.width = 1080f;

            Tmp = new();

            VecTmp = new Vector3(0.3f * Rect.width, -0.3f * Rect.height, 0f);

            /*
            for (int i = 0; i < 50; i++)
            {
                Tmp.Enqueue(
                    new AnimeClip(
                        Game.Inst.GetGameTime() + 10 * i,
                        Game.Inst.GetGameTime() + 5f + 10 * i,
                        VecTmp,
                        VecTmp - new Vector3(0f, 0.1f * Rect.height, 0f)
                    )
                );

                Tmp.Enqueue(
                    new AnimeClip(
                        Game.Inst.GetGameTime() + 5f + 10 * i,
                        Game.Inst.GetGameTime() + 10f + 10 * i,
                        VecTmp - new Vector3(0f, 0.1f * Rect.height, 0f),
                        VecTmp
                    )
                );
            }
            */

            Tmp.Enqueue(
                new AnimeClip(
                    Game.Inst.GetGameTime(),
                    Game.Inst.GetGameTime() + 114514f,
                    VecTmp,
                    VecTmp
                )
            );

            AnimeMachine Machine = new(Tmp);

            GetOneTrack(Page.Inst.CurPage, Machine, 3); // 默认使用第四条轨道按键作为预览按键 编号范围为 0 ~ 3
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="NoteBehaviour"/> 对象，并同时初始化它的动画机和母轨
        /// </summary>
        private void GetOneNote(AnimeMachine machine, TrackBehaviour track, float judgeTime)
        {
            var note = NotePool.Get().GetComponent<NoteBehaviour>().Init(machine, track, judgeTime);

            ActiveNoteList.Add(note);

            NoteUIDIterator++;
        }

        private void GetOneHold(
            AnimeMachine Machine,
            TrackBehaviour track,
            float judgeTime,
            float releaseTime
        )
        {
            var hold = HoldPool
                .Get()
                .GetComponent<HoldBehaviour>()
                .Init(Machine, track, judgeTime, releaseTime - judgeTime);

            ActiveHoldList.Add(hold);

            HoldUIDIterator++;
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="TrackBehaviour"/> 对象，并同时初始化它的动画机
        /// </summary>
        private void GetOneTrack(BaseUIPage Page, AnimeMachine Machine, int trackNumber = -1)
        {
            if (TrackUIDIterator < 4)
            {
                Machine.IsDestroyable = false;

                if (trackNumber != -1)
                {
                    var Track = TrackPool
                        .Get()
                        .GetComponent<TrackBehaviour>()
                        .Init(Page, Machine, TrackInputProvider, trackNumber);

                    ActiveTracks.Add(trackNumber, Track);
                }
                else
                {
                    var Track = TrackPool
                        .Get()
                        .GetComponent<TrackBehaviour>()
                        .Init(Page, Machine, TrackInputProvider, TrackUIDIterator);

                    ActiveTracks.Add(TrackUIDIterator, Track);

                    TrackUIDIterator++;
                }
            }
        }

        private void GetOneJudge(BaseUIPage parentPage, JudgeEnum judgeEnum)
        {
            JudgePool.Get().GetComponent<JudgeBehaviour>().Init(parentPage, judgeEnum);
        }

        public void ExitGame()
        {
            ActiveTracks.Clear();
            TrackPool.Clear();
            HoldPool.Clear();
            NotePool.Clear();

            TrackInputProvider.Disable();

            NoteUIDIterator = 0;
            HoldUIDIterator = 0;
            TrackUIDIterator = 0;
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

        public int GetNoteUIDIterator()
        {
            return NoteUIDIterator;
        }

        public int GetNotePoolCountInactive()
        {
            return NotePool.CountInactive;
        }
    }
}
