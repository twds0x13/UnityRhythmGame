using System.Collections.Generic;
using Anime;
using NoteNamespace;
using Singleton;
using TrackNamespace;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Pool;
using Ctrl = GameCore.GameController;
using Game = GameManager.GameManager;

namespace PooledObject
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
        Dictionary<int, TrackBehaviour> BaseTracks = new();

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
            InitPools();
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
                    Destroy(Note);
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
                    Destroy(Track);
                },
                true,
                8,
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

        public void GetNotesDynamic(int Num, float Duration)
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();

                float Time = Game.Inst.GetGameTime();

                Tmp.Enqueue(
                    new AnimeClip(
                        Time,
                        Time + Duration,
                        new Vector3(0f, 3.5f, 0f),
                        new Vector3(0f, 0f, 0f)
                    )
                );

                AnimeMachine Machine = new(Tmp);

                Machine.DisappearTimeSpan = 0.5f; // 超过 500ms 自动销毁

                TrackBehaviour Object;

                if (BaseTracks.TryGetValue(Num, out Object))
                {
                    GetOneNote(Machine, Object, Time + Duration);
                }
            }
        }

        public void GetTracksDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();
                Vector3 VecTmp;

                VecTmp = new Vector3(-0.75f + 0.5f * TrackUIDIterator, -1f, 0f);

                for (int i = 0; i < 50; i++)
                {
                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + 10 * i,
                            Game.Inst.GetGameTime() + 5f + 10 * i,
                            VecTmp,
                            VecTmp - new Vector3(0f, -0.2f, 0f)
                        )
                    );

                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + 5f + 10 * i,
                            Game.Inst.GetGameTime() + 10f + 10 * i,
                            VecTmp - new Vector3(0f, -0.2f, 0f),
                            VecTmp
                        )
                    );
                }

                AnimeMachine Machine = new(Tmp);

                GetOneTrack(Machine);
            }
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="NoteBehaviour"/> 对象，并同时初始化它的动画机和母轨
        /// </summary>
        private void GetOneNote(AnimeMachine Machine, TrackBehaviour Track, float JudgeTime)
        {
            // 新建一个 Note 对象，然后把 Init 返回的 Note 对象加入到那个 Track 的判定队列里面。

            //Track.JudgeQueue.Enqueue(
            NotePool.Get().GetComponent<NoteBehaviour>().Init(Machine, Track, JudgeTime);
            //);

            NoteUIDIterator++;
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="TrackBehaviour"/> 对象，并同时初始化它的动画机
        /// </summary>
        private void GetOneTrack(AnimeMachine Machine)
        {
            var Track = TrackPool
                .Get()
                .GetComponent<TrackBehaviour>()
                .Init(Machine, TrackUIDIterator);

            if (TrackUIDIterator < 4)
            {
                Machine.IsDestroyable = false;
                BaseTracks.Add(TrackUIDIterator, Track);
            }

            TrackUIDIterator++;
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
