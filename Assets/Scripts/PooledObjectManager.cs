using System.Collections.Generic;
using Anime;
using NoteNamespace;
using Singleton;
using TrackNamespace;
using UnityEngine;
using UnityEngine.Pool;
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
        List<TrackBehaviour> BaseTracks;

        [SerializeField]
        GameObject NoteInst;

        [SerializeField]
        GameObject TrackInst;

        [SerializeField]
        int NoteUIDIterator; // 你应该没同时用到21亿个 Note, 对吧？

        [SerializeField]
        int TrackUIDIterator; // 这两个计数器不会降低，只会递增

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

        public void GetNotesDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();

                for (int i = 0; i < 1; i++)
                {
                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + i / 5f,
                            Game.Inst.GetGameTime() + 1.25f + i / 5f,
                            new Vector3(0f, 3.5f, 0f),
                            new Vector3(0f, 0f, 0f)
                        )
                    );
                }

                AnimeMachine Machine = new(Tmp);

                if (TrackUIDIterator > 0)
                    GetOneNote(Machine, BaseTracks.Find(T => T.TrackNumber == 0));
            }
        }

        public void GetTracksDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();
                Vector3 VecTmp;

                VecTmp = new Vector3(-0.75f + 0.2f * TrackUIDIterator, -1f, 0f);

                for (int i = 0; i < 50; i++)
                {
                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + 10 * i,
                            Game.Inst.GetGameTime() + 5f + 10 * i,
                            VecTmp,
                            VecTmp - new Vector3(0f, -1.5f, 0f)
                        )
                    );

                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime() + 5f + 10 * i,
                            Game.Inst.GetGameTime() + 10f + 10 * i,
                            VecTmp - new Vector3(0f, -1.5f, 0f),
                            VecTmp
                        )
                    );
                }

                AnimeMachine Machine = new(Tmp);

                if (TrackUIDIterator < 4)
                    Machine.IsDestroyable = true;

                GetOneTrack(Machine);
            }
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="NoteBehaviour"/> 对象，并同时初始化它的动画队列
        /// </summary>
        private void GetOneNote(AnimeMachine Machine, TrackBehaviour Track)
        {
            NotePool.Get().GetComponent<NoteBehaviour>().Init(Machine, Track); // 这里以前有薏苡行代码

            NoteUIDIterator++;
        }

        /// <summary>
        /// 从对象池中获取一个新的 <see cref="TrackBehaviour"/> 对象，并同时初始化它的动画队列
        /// </summary>
        private void GetOneTrack(AnimeMachine Machine)
        {
            var Track = TrackPool.Get();

            Track.GetComponent<TrackBehaviour>().Init(Machine, TrackUIDIterator);

            if (TrackUIDIterator < 4)
                BaseTracks.Add(Track.GetComponent<TrackBehaviour>());

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

        public int GetTrackUIDIterator()
        {
            return TrackUIDIterator;
        }

        public int GetNotePoolCountInactive()
        {
            return NotePool.CountInactive;
        }
    }
}
