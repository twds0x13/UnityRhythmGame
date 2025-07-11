using System.Collections;
using System.Collections.Generic;
using Anime;
using GameCore;
using NoteManager;
using PooledObject;
using TrackManager;
using UnityEngine;
using UnityEngine.Pool;
using Game = GameManager.GameManager;

namespace PooledObject
{
    /// <summary>
    /// ����Ϸ�����ж�̬��������� <see cref="NoteBehaviour"/> ������Ҫ��ȡ�����ļ���Ϊ�����б�
    /// TODO : ʵ�ֶ�ȡ�ļ��ͽṹ���洢Note��Ϣ
    /// TODO ?: ����.mcz
    /// </summary>
    public class PooledObjectManager : MonoBehaviour
    {
        private PooledObjectManager() { } // ����ģʽ

        public static PooledObjectManager Inst { get; private set; }

        public ObjectPool<GameObject> NotePool;

        public ObjectPool<GameObject> TrackPool;

        public GameObject NoteInst;

        public GameObject TrackInst;

        public int NoteUIDIterator; // ��Ӧ��ûͬʱ�õ�21�ڸ� Note ���԰ɣ�

        public int TrackUIDIterator; // ���������������ή�ͣ�ֻ�����

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

        private void Awake()
        {
            InitInstance();

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
                            Game.Inst.GetGameTime() + 0.2f + i / 5f,
                            FlatRandVec(),
                            FlatRandVec()
                        )
                    );
                }

                AnimeMachine Machine = new(Tmp);

                GetOneNote(Machine);
            }
        }

        public void GetTracksDynamic()
        {
            if (!Game.Inst.IsGamePaused())
            {
                Queue<AnimeClip> Tmp = new();
                Vector3 VecTmp;

                VecTmp = new Vector3(-0.75f + 0.5f * TrackUIDIterator, 0f, 0f);

                for (int i = 0; i < 50; i++)
                {
                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime(),
                            Game.Inst.GetGameTime() + 5f,
                            VecTmp,
                            VecTmp - new Vector3(0f, 1f, 0f)
                        )
                    );

                    Tmp.Enqueue(
                        new AnimeClip(
                            Game.Inst.GetGameTime(),
                            Game.Inst.GetGameTime() + 5f,
                            VecTmp,
                            VecTmp - new Vector3(0f, 1f, 0f)
                        )
                    );
                }

                AnimeMachine Machine = new(Tmp);

                GetOneTrack(Machine);
            }
        }

        /// <summary>
        /// �Ӷ�����л�ȡһ���µ� <see cref="NoteBehaviour"/> ���󣬲�ͬʱ��ʼ�����Ķ�������
        /// </summary>
        private void GetOneNote(AnimeMachine Machine)
        {
            NotePool.Get().GetComponent<NoteBehaviour>().Init(Machine); // ������ǰ��޲���д���

            NoteUIDIterator++;
        }

        private void GetOneTrack(AnimeMachine Machine)
        {
            TrackPool.Get().GetComponent<TrackBehaviour>().Init(Machine);

            TrackUIDIterator++;
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
