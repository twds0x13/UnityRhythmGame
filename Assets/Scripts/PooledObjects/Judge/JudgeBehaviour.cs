using System.Collections.Generic;
using Anime;
using GameManagerNS;
using InterpNS;
using JudgeNS;
using PageNS;
using PooledObjectNS;
using UnityEngine;
using UnityEngine.Events;
using Game = GameManagerNS.GameManager;

public class JudgeBehaviour : ScaleableSpriteBehaviour
{
    public Sprite CriticalPerfect;

    public Sprite Perfect;

    public Sprite Great;

    public Sprite Good;

    public Sprite Miss;

    public AnimeMachine AnimeMachine;

    public UnityEvent DestroyEvent;

    public JudgeBehaviour Init(BaseUIPage parentPage, JudgeEnum judgeEnum)
    {
        transform.SetParent(parentPage.transform, false);

        switch (judgeEnum)
        {
            case JudgeEnum.CriticalPerfect:
                SpriteRenderer.sprite = CriticalPerfect;
                break;

            case JudgeEnum.Perfect:
                SpriteRenderer.sprite = Perfect;
                break;

            case JudgeEnum.Great:
                SpriteRenderer.sprite = Great;
                break;

            case JudgeEnum.Good:
                SpriteRenderer.sprite = Good;
                break;

            case JudgeEnum.Miss:
                SpriteRenderer.sprite = Miss;
                break;

            case JudgeEnum.NotEntered:
                SpriteRenderer.sprite = Miss;
                break;
        }

        SetScale(Vector3.one * 45f, ScaleMode.FitToHeight);

        SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);

        Rect rect = ResizeDetector.Inst.Rect.rect;

        var startTime = Game.Inst.GetGameTime();

        var animeQueue = new Queue<AnimeClip>();

        var startPoint = new Vector3(0f, 0f, 0f);

        var animeClip = new AnimeClip(
            startTime,
            startTime + 0.175f,
            startPoint
                + new Vector3(0f, (0.15f + GameSettings.JudgeDisplayerHeight) * rect.height, 0f),
            startPoint
                + new Vector3(0f, (0.05f + GameSettings.JudgeDisplayerHeight) * rect.height, 0f)
        );

        animeQueue.Enqueue(animeClip);

        AnimeMachine = new(animeQueue);

        return this;
    }

    public void Update()
    {
        AnimeManager();
    }

    public void AnimeManager()
    {
        AnimeMachine.AnimeQueue.TryPeek(out AnimeMachine.CurAnime); // 至少 "应该" 有一个垫底动画

        if (Game.Inst.GetGameTime() < AnimeMachine.CurAnime.EndT)
        {
            UpdateAnime();
        }
        else
        {
            if (!AnimeMachine.AnimeQueue.TryDequeue(out AnimeMachine.CurAnime))
            {
                DestroyEvent?.Invoke();

                transform.position = new Vector3(0f, 20f, 0f);

                SpriteRenderer.color = Color.clear;

                SpriteRenderer.sprite = null;
            }
        }
    }

    private void UpdateAnime()
    {
        AnimeMachine.CurT =
            (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
            / AnimeMachine.CurAnime.TotalTimeElapse;

        transform.localPosition = InterpFunc.VectorHandler(
            AnimeMachine.CurAnime.StartV,
            AnimeMachine.CurAnime.EndV,
            AnimeMachine.CurT,
            AxisFunc.Linear,
            AxisFunc.Pow,
            AxisFunc.Linear,
            PowY: 1.75f
        );

        SpriteRenderer.color = new Color(
            1f,
            1f,
            1f,
            Mathf.Lerp(1f, 0f, Mathf.Pow(AnimeMachine.CurT, 0.5f))
        );
    }
}
