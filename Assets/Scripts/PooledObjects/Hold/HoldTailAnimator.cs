using System.Collections;
using System.Collections.Generic;
using Anime;
using InterpNS;
using TrackNS;
using UnityEngine;
using Game = GameManagerNS.GameManager;

namespace HoldNS
{
    public class HoldTailAnimator : MonoBehaviour, IVertical
    {
        [SerializeField]
        private TrackBehaviour parentTrack; // 持有尾部的对象

        [SerializeField]
        private HoldBehaviour parentHold; // 持有尾部的对象

        [SerializeField]
        private AnimeMachine animeMachine; // 尾部的动画机，用来驱动尾部动画

        [SerializeField]
        private SpriteRenderer spriteRenderer; // 用于显示尾部图片

        public float Vertical { get; set; } = 1f; // 纵向位置缩放

        public enum ScaleMode
        {
            Stretch, // 拉伸填充
            FitToWidth, // 适应宽度
            FitToHeight, // 适应高度
            FitInside, // 适应内部（不超出）
            FitOutside, // 适应外部（完全填充）
        }

        public void Init(AnimeMachine machine, HoldBehaviour hold)
        {
            animeMachine = machine;
            parentHold = hold;
            parentTrack = hold.ParentTrack;

            transform.SetParent(parentTrack.transform, true);
            SetScale(Vector3.one * 0.110f);

            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }

        public virtual void SetScale(Vector3 scale)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                LogManager.Warning("SpriteRenderer 或 Sprite 为空，无法进行缩放");
                return;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            Vector3 newScale = CalculateScale(spriteSize, scale, ScaleMode.FitToWidth);
            transform.localScale = newScale;
        }

        /// <summary>
        /// 根据指定的模式计算缩放比例
        /// </summary>
        private Vector3 CalculateScale(Vector2 originalSize, Vector2 targetSize, ScaleMode mode)
        {
            Vector3 scale = Vector3.one;

            switch (mode)
            {
                case ScaleMode.Stretch:
                    scale.x = targetSize.x / originalSize.x;
                    scale.y = targetSize.y / originalSize.y;
                    break;

                case ScaleMode.FitToWidth:
                    float widthScale = targetSize.x / originalSize.x;
                    scale.x = widthScale;
                    scale.y = widthScale;
                    break;

                case ScaleMode.FitToHeight:
                    float heightScale = targetSize.y / originalSize.y;
                    scale.x = heightScale;
                    scale.y = heightScale;
                    break;

                case ScaleMode.FitInside:
                    float scaleX = targetSize.x / originalSize.x;
                    float scaleY = targetSize.y / originalSize.y;
                    float minScale = Mathf.Min(scaleX, scaleY);
                    scale.x = minScale;
                    scale.y = minScale;
                    break;

                case ScaleMode.FitOutside:
                    float scaleX2 = targetSize.x / originalSize.x;
                    float scaleY2 = targetSize.y / originalSize.y;
                    float maxScale = Mathf.Max(scaleX2, scaleY2);
                    scale.x = maxScale;
                    scale.y = maxScale;
                    break;
            }

            return scale;
        }

        private void Update()
        {
            AnimeManager();
        }

        public void AnimeManager()
        {
            animeMachine.AnimeQueue.TryPeek(out animeMachine.CurAnime); // 至少 "应该" 有一个垫底动画

            if (Game.Inst.GetGameTime() < animeMachine.CurAnime.EndT)
            {
                UpdatePosition();
            }
            else
            {
                if (!animeMachine.AnimeQueue.TryDequeue(out animeMachine.CurAnime))
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0f); // 隐藏尾部
                }
            }
        }

        public void UpdatePosition()
        {
            animeMachine.CurT =
                (Game.Inst.GetGameTime() - animeMachine.CurAnime.StartT)
                / animeMachine.CurAnime.TotalTimeElapse;

            transform.position =
                Vertical
                    * InterpFunc.VectorHandler(
                        animeMachine.CurAnime.StartV,
                        animeMachine.CurAnime.EndV,
                        animeMachine.CurT,
                        AxisFunc.Linear,
                        AxisFunc.Linear,
                        AxisFunc.Linear
                    )
                + parentTrack.transform.position;

            spriteRenderer.color = parentHold.SpriteRenderer.color; // 同步颜色
        }
    }
}
