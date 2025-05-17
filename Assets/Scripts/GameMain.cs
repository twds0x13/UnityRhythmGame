using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BaseObject;
using GBehaviour;
using IUtils;
using NBehaviour;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using UnityEngine;
using UnityEngine.Pool;
using Vectors;
using Random = UnityEngine.Random;

public class GameMain : MonoBehaviour, IDev
{
    private ObjectPool<Queue<VectorPair>> VectorQueuePool;

    private ObjectPool<GameObject> NotePool;

    public GameObject NoteInst; // 不同颜色的Note已合并为同一个游戏物件，用不同的Sprite表示

    GameObject CurObj;

    NoteBehaviour CurNote;

    Queue<VectorPair> CurQueue;

    public bool CurFlag;

    public bool TestFlag;

    public int CurNoteCount;

    private void InitPools()
    {
        VectorQueuePool = new ObjectPool<Queue<VectorPair>>(
            () =>
            {
                return new Queue<VectorPair>();
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
                        Note.GetComponent<NoteBehaviour>().AnimeQueue = null;

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
            maxSize: 256
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

            TestFlag = !TestFlag;
        }
    }

    /// <summary>
    /// 用来从对象池中获取新的 <see cref="NoteBehaviour"/> 对象，已初始化动画队列
    ///
    /// Todo:支持异步调用
    /// </summary>
    /// <param name="CurFlag">在当前 Note 完成初始化前这个 Flag 应该为 false（按引用传递）</param>
    public void OneNote()
    {
        // Debug.LogFormat(
        //     "Start Note {0} Formatting. Timestamp: {1} ",
        //     CurNoteCount,
        //     GameBehaviour.Inst.AbsTime()
        // );

        CurObj = NotePool.Get();

        CurNote = CurObj.GetComponent<NoteBehaviour>();

        CurQueue = CurNote.AnimeQueue;

        for (int i = 0; i < 100; i++)
        {
            CurQueue.Enqueue(
                new VectorPair(
                    new Vector3(Random.value, Random.value, Random.value),
                    new Vector3(Random.value, Random.value, Random.value),
                    i,
                    i + 1
                )
            );
            CurObj.GetComponent<NoteBehaviour>().AnimeQueue.Dequeue().DevLog();
        }

        // Debug.LogFormat(
        //     "Note {0} Finished Formatting. Timestamp: {1} ",
        //     CurNoteCount,
        //     GameBehaviour.Inst.AbsTime()
        // );

        CurNoteCount++;
    }

    /// <summary>
    /// Todo:添加异步处理
    /// </summary>
    public void CreateAllNotes()
    {
        Debug.LogFormat(
            "Start Note Creating Process. AbsTime: {0} ms",
            GameBehaviour.Inst.AbsTimeMs()
        );

        for (int i = 0; i < 1; i++)
        {
            OneNote();
        }

        Debug.LogFormat(
            "End Note Creating Process. AbsTime: {0} ms",
            GameBehaviour.Inst.AbsTimeMs()
        );
    }

    public void DevLog()
    {
        GameBehaviour.Inst.DevLog();
    }
}
