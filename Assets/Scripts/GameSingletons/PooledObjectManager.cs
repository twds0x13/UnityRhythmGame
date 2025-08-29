using System.Collections.Generic;
using Anime;
using NoteNS;
using PageNS;
using Singleton;
using TrackNS;
using UnityEngine;
using UnityEngine.Pool;
using Game = GameManagerNS.GameManager;
using Page = UIManagerNS.PageController;

namespace PooledObjectNS
{
    /// <summary>
    /// 在游戏过程中动态生成所需的 <see cref="PooledObjectBehaviour"/> 对象，需要读取谱面文件作为需求列表。
    /// TODO : 实现读取文件和结构化存储Note信息
    /// TODO ?: 适配.mcz
    /// </summary>
    public class PooledObjectManager : Singleton<PooledObjectManager>
    {
        [SerializeField]
        ObjectPool<GameObject> NotePool;

        [SerializeField]
        ObjectPool<GameObject> TrackPool;

        [SerializeField]
        Dictionary<int, TrackBehaviour> BaseTracks;

        [SerializeField]
        GameObject NoteInst;

        [SerializeField]
        GameObject TrackInst;

        [SerializeField]
        public int NoteUIDIterator { get; private set; } = 0; // 你应该没同时用到21亿个 Note, 对吧？

        [SerializeField]
        public int TrackUIDIterator { get; private set; } = 0; // 这两个计数器不会降低，只会递增

        protected override void SingletonAwake()
        {
            InitTrackDict();
            InitPools();
        }

        public void InitTrackDict()
        {
            BaseTracks = new();
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
                },
                (Note) =>
                {
                    DestroyImmediate(Note);
                },
                true,
                128,
                1024
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
                    DestroyImmediate(Track);
                },
                true,
                4,
                32 - 4 // 这里的 maxSize 只限制池内未启用物体数量....
            );
        }

        private GameObject InstantiateTrack()
        {
            GameObject Track = Instantiate(TrackInst, transform);

            Track.GetComponent<PooledObjectBehaviour>().transform.position = new Vector3(
                0f,
                20f,
                0f
            );

            Track
                .GetComponent<PooledObjectBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    TrackPool.Release(Track);
                });

            return Track;
        }

        private GameObject InstantiateNote()
        {
            GameObject Note = Instantiate(NoteInst, transform);

            Note.GetComponent<PooledObjectBehaviour>().transform.position = new Vector3(
                0f,
                20f,
                0f
            );

            Note.GetComponent<PooledObjectBehaviour>()
                .DestroyEvent.AddListener(() =>
                {
                    NotePool.Release(Note);
                });

            return Note;
        }

        public void GetNotesDynamic(float StartTime, float Vertical, int TrackNum, float Duration)
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();

                Rect Rect = Page.Inst.CurPage.GetRect();

                Rect.height = 1080f;

                Tmp.Enqueue(
                    new AnimeClip(
                        StartTime,
                        StartTime + Duration,
                        new Vector3(0f, Rect.height * Vertical, 0f),
                        new Vector3(0f, 0f, 0f)
                    )
                );

                AnimeMachine Machine = new(Tmp);

                Machine.HasJudgeAnime = true; // 切换判定动画开关

                TrackBehaviour Object;

                if (BaseTracks.TryGetValue(TrackNum, out Object))
                {
                    GetOneNote(Machine, Object, StartTime + Duration);
                }
            }
        }

        public void GetTracksDynamic()
        {
            Queue<AnimeClip> Tmp;
            Vector3 VecTmp;
            Rect Rect = Page.Inst.CurPage.GetRect();
            Rect.width = 1080f;
            Rect.height = 1080f;

            for (int j = 0; j < 4; j++)
            {
                Tmp = new();

                VecTmp = new Vector3(
                    -0.3f * Rect.width + 0.2f * Rect.width * TrackUIDIterator,
                    -0.35f * Rect.height,
                    0f
                );

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

                AnimeMachine Machine = new(Tmp);

                GetOneTrack(Page.Inst.CurPage, Machine);
            }
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="NoteBehaviour"/> 对象，并同时初始化它的动画机和母轨
        /// </summary>
        private void GetOneNote(AnimeMachine Machine, TrackBehaviour Track, float JudgeTime)
        {
            NotePool.Get().GetComponent<NoteBehaviour>().Init(Machine, Track, JudgeTime);

            NoteUIDIterator++;
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="TrackBehaviour"/> 对象，并同时初始化它的动画机
        /// </summary>
        private void GetOneTrack(BaseUIPage Page, AnimeMachine Machine)
        {
            var Track = TrackPool
                .Get()
                .GetComponent<TrackBehaviour>()
                .Init(Page, Machine, TrackUIDIterator);

            if (TrackUIDIterator < 4)
            {
                Machine.IsDestroyable = false;
                BaseTracks.Add(TrackUIDIterator, Track);
            }

            TrackUIDIterator++;
        }

        public void FinishCurrentGame()
        {
            BaseTracks.Clear();
            NoteUIDIterator = 0;
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
