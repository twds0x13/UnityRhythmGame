using System.Collections;
using System.Collections.Generic;
using Anime;
using InterpNS;
using PooledObjectNS;
using TrackNS;
using UnityEngine;
using Game = GameManagerNS.GameManager;

namespace HoldNS
{
    public class HoldTailAnimator : ScaleableSpriteBehaviour, IVertical
    {
        [SerializeField]
        private TrackBehaviour parentTrack; // 持有尾部的对象

        [SerializeField]
        private HoldBehaviour parentHold; // 持有尾部的对象

        public AnimeMachine AnimeMachine; // 尾部的动画机，用来驱动尾部动画

        public float Vertical { get; set; } = 1f; // 纵向位置缩放

        public Pack VerticalCache { get; private set; } = new(default, 0f);

        public void Init(AnimeMachine machine, HoldBehaviour hold)
        {
            AnimeMachine = machine;
            parentHold = hold;
            parentTrack = hold.ParentTrack;

            transform.SetParent(parentTrack.transform, true);
            SetScale(Vector3.one * 0.110f);

            SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }

        private void Update()
        {
            AnimeManager();
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
                    SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // 隐藏尾部
                }
            }
        }

        public void UpdatePosition()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                / AnimeMachine.CurAnime.TotalTimeElapse;

            if (!parentHold.IsDisappearing) // 在消失的时候由 HoldBehaviour 临时接管 Tail 位移，不考虑脚本执行顺序等
            {
                transform.position =
                    Vertical
                        * InterpFunc.VectorHandler(
                            AnimeMachine.CurAnime.StartV,
                            AnimeMachine.CurAnime.EndV,
                            AnimeMachine.CurT,
                            AxisFunc.Linear,
                            AxisFunc.Pow,
                            AxisFunc.Linear,
                            PowY: 1.00f
                        )
                    + parentTrack.transform.position;
            }

            transform.localScale = parentHold.transform.localScale;

            SpriteRenderer.sprite = parentHold.SpriteRenderer.sprite; // 同步贴图

            SpriteRenderer.color = parentHold.SpriteRenderer.color; // 同步颜色
        }

        public void UpdateCache()
        {
            VerticalCache = new(transform.position, Game.Inst.GetGameTime());
        }
    }
}
