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
        List<TrackBehaviour> BaseTracks;

        [SerializeField]
        GameObject NoteInst;

        [SerializeField]
        GameObject TrackInst;

        [SerializeField]
        int NoteUIDIterator; // ��Ӧ��ûͬʱ�õ�21�ڸ� Note, �԰ɣ�

        [SerializeField]
        int TrackUIDIterator; // ���������������ή�ͣ�ֻ�����

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
        /// �Ӷ�����л�ȡһ���µ� <see cref="NoteBehaviour"/> ���󣬲�ͬʱ��ʼ�����Ķ�������
        /// </summary>
        private void GetOneNote(AnimeMachine Machine, TrackBehaviour Track)
        {
            NotePool.Get().GetComponent<NoteBehaviour>().Init(Machine, Track); // ������ǰ��޲���д���

            NoteUIDIterator++;
        }

        /// <summary>
        /// �Ӷ�����л�ȡһ���µ� <see cref="TrackBehaviour"/> ���󣬲�ͬʱ��ʼ�����Ķ�������
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
