using System.Collections.Generic;
using Anime;
using NoteNS;
using Singleton;
using TrackNS;
using UnityEngine;
using UnityEngine.Pool;
using Game = GameManagerNS.GameManager;
using Page = UserInterfaceNS.UserInterfaceManager;

namespace PooledObjectNS
{
    /// <summary>
    /// ����Ϸ�����ж�̬��������� <see cref="PooledObjectBehaviour"/> ������Ҫ��ȡ�����ļ���Ϊ�����б�
    /// TODO : ʵ�ֶ�ȡ�ļ��ͽṹ���洢Note��Ϣ
    /// TODO ?: ����.mcz
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
        public int NoteUIDIterator { get; private set; } = 0; // ��Ӧ��ûͬʱ�õ�21�ڸ� Note, �԰ɣ�

        [SerializeField]
        public int TrackUIDIterator { get; private set; } = 0; // ���������������ή�ͣ�ֻ�����

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
                32 - 4 // ����� maxSize ֻ���Ƴ���δ������������....
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

                Tmp.Enqueue(
                    new AnimeClip(
                        StartTime,
                        StartTime + Duration,
                        new Vector3(0.3f - 0.2f * TrackNum, 2.2f * Vertical, 2f),
                        new Vector3(0f, 0f, 0f)
                    )
                );

                AnimeMachine Machine = new(Tmp);

                Machine.HasJudgeAnime = true; // �л��ж���������

                TrackBehaviour Object;

                if (BaseTracks.TryGetValue(TrackNum, out Object))
                {
                    GetOneNote(Machine, Object, StartTime + Duration);
                }
            }
        }

        public void GetTracksDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp;
                Vector3 VecTmp;

                for (int j = 0; j < 4; j++)
                {
                    Tmp = new();

                    VecTmp = new Vector3(-0.75f + 0.5f * TrackUIDIterator, -1.2f, 0f);

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

                    GetOneTrack(Page.Inst.CurPage.Peek(), Machine);
                }
            }
        }

        /// <summary>
        /// �Ӷ�����л�ȡһ���µ� <see cref="NoteBehaviour"/> ���󣬲�ͬʱ��ʼ�����Ķ�������ĸ��
        /// </summary>
        private void GetOneNote(AnimeMachine Machine, TrackBehaviour Track, float JudgeTime)
        {
            // �½�һ�� Note ����Ȼ��� Init ���ص� Note ������뵽�Ǹ� Track ���ж��������档

            //Track.JudgeQueue.Enqueue(
            NotePool.Get().GetComponent<NoteBehaviour>().Init(Machine, Track, JudgeTime);
            //);

            NoteUIDIterator++;
        }

        /// <summary>
        /// �Ӷ�����л�ȡһ���µ� <see cref="TrackBehaviour"/> ���󣬲�ͬʱ��ʼ�����Ķ�����
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

        public void ResetGame()
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
