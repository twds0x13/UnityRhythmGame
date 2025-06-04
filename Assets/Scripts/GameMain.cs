using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anime;
using Cysharp.Threading.Tasks;
using IUtils;
using NBehaviour;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.WSA;
using Game = GameBehaviourManager.GameBehaviour;
using Rand = UnityEngine.Random;

public class GameMain : MonoBehaviour, IDev
{
    private ObjectPool<ConcurrentQueue<AnimeClip>> VectorQueuePool;

    private ObjectPool<GameObject> NotePool;

    public GameObject NoteInst; // 不同颜色的Note已合并为同一个游戏物件，用不同的Sprite表示

    AnimeClip CurAnime;

    public bool CurFlag;

    public bool TestFlag;

    public int CurNoteCount;

    private void InitPools()
    {
        VectorQueuePool = new ObjectPool<ConcurrentQueue<AnimeClip>>(
            () =>
            {
                return new ConcurrentQueue<AnimeClip>();
            },
            null,
            (VQueue) =>
            {
                VQueue.Clear();
            },
            (VQueue) =>
            {
                VQueue = null;
            },
            maxSize: 512
        );

        NotePool = new ObjectPool<GameObject>(
            () =>
            {
                GameObject Note = Instantiate(NoteInst, transform);

                Note.GetComponent<NoteBehaviour>().AnimeQueue = VectorQueuePool.Get();

                Note.GetComponent<NoteBehaviour>()
                    .DestroyEvent.AddListener(() =>
                    {
                        VectorQueuePool.Release(Note.GetComponent<NoteBehaviour>().AnimeQueue);

                        Destroy(Note);
                    });

                return Note;
            },
            (Note) =>
            {
                Note.SetActive(true);
            },
            (Note) =>
            {
                VectorQueuePool.Release(Note.GetComponent<NoteBehaviour>().AnimeQueue);

                Note.SetActive(false);
            },
            (Note) =>
            {
                Note.GetComponent<NoteBehaviour>().AnimeQueue = null;

                Destroy(Note);
            },
            maxSize: 512
        );
    }

    void Start()
    {
        InitPools();

        DevLog();

        TestFlag = true;
    }

    void Update()
    {
        if (TestFlag)
        {
            CreateAllNotes();
        }
        TestFlag = false;
    }

    public void InitUserInterface() { }

    /// <summary>
    /// 从对象池中获取一个·新的 <see cref="NoteBehaviour"/> 对象，并同时初始化动画队列
    /// </summary>
    /// <param name="CurFlag">在当前 Note 完成初始化前这个 Flag 应该为 false（按引用传递）</param>
    public void OneNote()
    {
        float step = 0.5f;

        GameObject CurObj = NotePool.Get();

        for (float i = 0f; i < 1000f; i += step)
        {
            CurObj
                .GetComponent<NoteBehaviour>()
                .AnimeQueue.Enqueue(new AnimeClip(FlatRandVec(), FlatRandVec(), i, i + step));
        }

        //CurObj.GetComponent<NoteBehaviour>().AnimeQueue.TryDequeue(out CurAnime);
        //CurAnime.DevLog();

        CurNoteCount++;
    }

    /// <summary>
    /// 一次性生成所有Note
    /// </summary>
    public void CreateAllNotes()
    {
        float First;

        float End;

        First = Game.Inst.AbsTimeMs();

        Debug.LogFormat("Start Note Creating Process. AbsTime: {0} ms", First);

        for (int i = 0; i < 10; i++)
        {
            OneNote();
        }

        End = Game.Inst.AbsTimeMs();

        Debug.LogFormat("End Note Creating Process. AbsTime: {0} ms", End);

        Debug.LogFormat("Creating Process Time Elapse : {0} ms", End - First);

        Debug.Log(NotePool.CountAll);
    }

    public Vector3 RandVec()
    {
        return new Vector3(
            (float)(3f * (Rand.value - 0.5)),
            (float)(2f * (Rand.value - 0.5)),
            (float)(2f * (Rand.value - 0.5))
        );
    }

    public Vector3 FlatRandVec()
    {
        return new Vector3((float)(3f * (Rand.value - 0.5)), (float)(2f * (Rand.value - 0.5)), 0f);
    }

    public void DevLog()
    {
        Game.Inst.DevLog();
    }
}
