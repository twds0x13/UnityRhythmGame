using Anime;
using InterpNS;
using JudgeNS;
using NoteNS;
using StateMachine;
using UnityEngine;
using Game = GameManagerNS.GameManager;
using Judge = JudgeNS.NoteJudge;
using Page = UIManagerNS.PageManager;

namespace NoteStateMachine
{
    public class NoteState : IState<NoteBehaviour>
    {
        protected NoteBehaviour Note;

        protected AnimeMachine AnimeMachine;

        protected LinearStateMachine<NoteBehaviour> StateMachine;

        public NoteState(NoteBehaviour Note, LinearStateMachine<NoteBehaviour> StateMachine)
        {
            this.Note = Note;

            this.StateMachine = StateMachine;

            this.AnimeMachine = Note.AnimeMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }

    public class StateBeforeJudgeNote : NoteState
    {
        public StateBeforeJudgeNote(
            NoteBehaviour Note,
            LinearStateMachine<NoteBehaviour> StateMachine
        )
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            // LogManager.Log("bf");
        }

        public override void Update()
        {
            base.Update();

            if (StartJudge)
            {
                StateMachine.SwitchState(Note.OnJudge);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private bool StartJudge =>
            Game.Inst.GetGameTime() < Note.JudgeTime
            && Judge.GetJudgeEnum(Note) != Judge.NoteJudgeEnum.NotEntered;
    }

    public class StateOnJudgeNote : NoteState
    {
        public StateOnJudgeNote(NoteBehaviour Note, LinearStateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            RegisterJudge(); // 可以在使用的时候才注册，这样能省掉 JudgeList 里的额外判断！这是好文明
        }

        public override void Update()
        {
            base.Update();

            if (ExitJudge)
            {
                StateMachine.SwitchState(Note.AfterJudge);
            }
        }

        public override void Exit()
        {
            base.Exit();

            UnregisterJudge();
        }

        // 在超过判定时间之后自动移除
        private bool ExitJudge =>
            Game.Inst.GetGameTime() > Note.JudgeTime
            && Judge.GetJudgeEnum(Note) == Judge.NoteJudgeEnum.Miss;

        private void RegisterJudge()
        {
            Note.ParentTrack.RegisterJudge(Note);
        }

        private void UnregisterJudge()
        {
            Note.ParentTrack.UnregisterJudge(Note);
        }
    }

    public class StateAfterJudgeNote : NoteState
    {
        public StateAfterJudgeNote(
            NoteBehaviour Note,
            LinearStateMachine<NoteBehaviour> StateMachine
        )
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            Game.Inst.Score.MaxScore += NoteJudgeScore.Max;
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }

    public class StateInitNote : NoteState
    {
        public StateInitNote(NoteBehaviour Note, LinearStateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            CombineParent(Note);

            AnimeInit(Note);

            StateMachine.SwitchState(Note.AnimeNote);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void CombineParent(NoteBehaviour Note)
        {
            Note.transform.SetParent(Note.ParentTrack.transform, true);

            Note.ParentTrack.Register(Note);
        }

        private void AnimeInit(NoteBehaviour Note)
        {
            Note.SpriteRenderer.sprite = Note.GetSprite("note_color");

            Note.SetScale(Vector3.one * 0.110f); // 魔法数字

            Note.SpriteRenderer.color =
                Note.ParentTrack.TrackNumber < 2
                    ? new Color(0.95f, 0.95f, 1f, 1f)
                    : new Color(1f, 0.95f, 0.95f, 1f);
        }
    }

    public class StateAnimeNote : NoteState
    {
        public StateAnimeNote(NoteBehaviour Note, LinearStateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
        }

        public override void Update()
        {
            base.Update();
            AnimeManager();
        }

        public override void Exit()
        {
            base.Exit();
        }

        public void UpdatePosition()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                / AnimeMachine.CurAnime.TotalTimeElapse;

            Note.transform.position =
                InterpFunc.VectorHandler(
                    AnimeMachine.CurAnime.StartV,
                    AnimeMachine.CurAnime.EndV,
                    AnimeMachine.CurT,
                    AxisFunc.Linear,
                    AxisFunc.Pow,
                    AxisFunc.Linear,
                    PowY: 1.25f
                ) * Note.Vertical
                + Note.ParentTrack.transform.position;
            ;
        }

        public void AnimeManager()
        {
            AnimeMachine.AnimeQueue.TryPeek(out AnimeMachine.CurAnime); // 至少 "应该" 有一个垫底动画

            if (Game.Inst.GetGameTime() < AnimeMachine.CurAnime.EndT)
            {
                UpdatePosition();
            }
            else
            {
                if (!AnimeMachine.AnimeQueue.TryDequeue(out AnimeMachine.CurAnime))
                {
                    if (AnimeMachine.HasDisappearAnime)
                    {
                        StateMachine.SwitchState(Note.DisappearNoteAnime);
                    }
                    else
                    {
                        StateMachine.SwitchState(Note.DestroyNoteAnime);
                    }
                }
            }
        }
    }

    public class StateJudgeAnimeNote : NoteState
    {
        public StateJudgeAnimeNote(
            NoteBehaviour Note,
            LinearStateMachine<NoteBehaviour> StateMachine
        )
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            AnimeMachine.JudgeCache = new Pack(Note.transform.position, Game.Inst.GetGameTime());
        }

        public override void Update()
        {
            base.Update();

            if (JudgeAnime())
            {
                StateMachine.SwitchState(Note.DestroyNoteAnime);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public bool JudgeAnime()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.JudgeTimeCache)
                / AnimeMachine.JudgeAnimeTimeSpan;

            Note.transform.position =
                new Vector3(0f, 0f * AnimeMachine.CurT, 0f) + AnimeMachine.JudgePosCache;

            if (AnimeMachine.HasJudgeAnime)
            {
                Note.SpriteRenderer.color = new Color(
                    1f,
                    1f,
                    1f - 0.5f * AnimeMachine.CurT,
                    1f - 2.5f * AnimeMachine.CurT
                );
            }
            else
            {
                StateMachine.SwitchState(Note.DestroyNoteAnime);
            }

            return AnimeMachine.CurT - 1f >= 0;
        }
    }

    public class StateDisappearNote : NoteState
    {
        public StateDisappearNote(
            NoteBehaviour Note,
            LinearStateMachine<NoteBehaviour> StateMachine
        )
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            AnimeMachine.DisappearCache = new Pack(
                Note.transform.position,
                Game.Inst.GetGameTime()
            );
        }

        public override void Update()
        {
            base.Update();

            if (Disappear())
            {
                StateMachine.SwitchState(Note.DestroyNoteAnime);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public bool Disappear()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.DisappearTimeCache)
                / AnimeMachine.DisappearTimeSpan;

            Note.transform.position =
                new Vector3(
                    0f,
                    -0.25f * ResizeDetector.Inst.Rect.rect.height * AnimeMachine.CurT,
                    0f
                ) + AnimeMachine.DisappearingPosCache;

            Note.SpriteRenderer.SetAlpha(0.3f - 0.3f * AnimeMachine.CurT);

            return AnimeMachine.CurT - 1f >= 0;
        }
    }

    public class StateDestroyNote : NoteState
    {
        public StateDestroyNote(NoteBehaviour Note, LinearStateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            AnimeExit();

            ParentExit();

            Note.DestroyEvent?.Invoke();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void ParentExit()
        {
            Note.ParentTrack.Unregister(Note);
            Note.ParentTrack.UnregisterJudge(Note);
        }

        private void AnimeExit()
        {
            Note.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            Note.transform.position = new Vector3(0f, 20f, 0f);
        }
    }
}
