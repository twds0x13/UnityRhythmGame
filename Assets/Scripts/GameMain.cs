using System.Collections.Generic;
using Anime;
using GameBehaviourManager;
using IUtils;
using NBehaviour;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Game = GameBehaviourManager.GameBehaviour;
using Rand = UnityEngine.Random;

public class GameMain : MonoBehaviour, IDev
{
    private static ObjectPool<GameObject> NotePool;

    public delegate void UpdateHandler();

    public UpdateHandler UpdateWhenKeyPressed;

    public static GameSettings GameSettings = new GameSettings();

    public System.Random RandInt = new System.Random();

    public GameObject NoteInst; // 不同颜色的Note已合并为同一个游戏物件，用不同的Sprite表示

    public bool CurFlag;

    public bool isDev;

    public bool TestFlag;

    public static int CurNoteCount;

    private void InitPools()
    {
        NotePool = new ObjectPool<GameObject>(
            () =>
            {
                GameObject Note = Instantiate(NoteInst, transform);

                Note.GetComponent<NoteBehaviour>()
                    .DestroyEvent.AddListener(() =>
                    {
                        // Egg Egg ......

                        NotePool.Release(Note);

                        CurNoteCount--;
                    });

                Note.GetComponent<NoteBehaviour>().ResetNote();

                return Note;
            },
            (Note) =>
            {
                Note.SetActive(true);

                Note.GetComponent<NoteBehaviour>().ResetNote();
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
            512,
            2048
        );
    }

    void Start()
    {
        InitPools();

        UpdateWhenKeyPressed += TestLoadGameSettings;

        UpdateWhenKeyPressed += Test;

        DevLog();

        TestFlag = true;
    }

    void Update()
    {
        UpdateWhenKeyPressed();
    }

    private void Test()
    {
        if (Input.GetKey(KeyCode.D))
        {
            GetNotesDynamic();
        }
    }

    private void TestLoadGameSettings()
    {
        if (Input.GetKeyDown(GameSettings.KeyGameTesting))
        {
            GameBehaviour.Inst.SaveGameSettings();
        }
    }

    /// <summary>
    /// 在游戏过程中动态生成所需的Note，需要读取谱面文件作为需求列表。
    /// TODO : 实现读取文件和结构化存储Note信息
    /// TODO ?: 适配.osz
    /// </summary>
    private void GetNotesDynamic()
    {
        if (!Game.Inst.IsGamePaused())
        {
            var Tmp = new Queue<AnimeClip>();
            var VecTmp = new Vector3(-1f, 1f, 0f);
            Tmp.Enqueue(
                new AnimeClip(
                    Game.Inst.GetGameTime(),
                    Game.Inst.GetGameTime() + 0.35f,
                    VecTmp,
                    VecTmp - new Vector3(0f, 1.5f, 0f)
                )
            );

            GetOneNote(Tmp);
            Tmp = new Queue<AnimeClip>();
            VecTmp = new Vector3(1f, 1f, 0f);
            Tmp.Enqueue(
                new AnimeClip(
                    Game.Inst.GetGameTime(),
                    Game.Inst.GetGameTime() + 0.35f,
                    VecTmp,
                    VecTmp - new Vector3(0f, 1.5f, 0f)
                )
            );
            GetOneNote(Tmp);
        }
    }

    /// <summary>
    /// 从对象池中获取一个·新的 <see cref="NoteBehaviour"/> 对象，并同时初始化动画队列
    /// </summary>
    /// <param name="CurFlag">在当前 Note 完成初始化前这个 Flag 应该为 false（按引用传递）</param>
    private void GetOneNote(Queue<AnimeClip> AnimeQueue)
    {
        GameObject CurObj = NotePool.Get();

        CurObj.GetComponent<NoteBehaviour>().AnimeQueue = AnimeQueue;

        CurNoteCount++;
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
        return new Vector3((float)(4f * (Rand.value - 0.5)), (float)(2f * (Rand.value - 0.5)), 0f);
    }

    public Vector3 StraightRandVec(float Y)
    {
        return new Vector3((float)(3f * (Rand.value - 0.5)), Y, (float)(Rand.value + 1f));
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
