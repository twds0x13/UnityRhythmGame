using System.Collections.Generic;
using Anime;
using UIManagerNS;
using UnityEngine;
using UnityEngine.Events;

namespace PooledObjectNS
{
    public class ScaleableSpriteBehaviour : MonoBehaviour
    {
        [Ext.ReadOnlyInGame, SerializeField]
        private List<Sprite> SpriteList; // 最好是非空的

        private readonly Dictionary<string, Sprite> _sprites = new();

        public SpriteRenderer SpriteRenderer;

        private ScaleMode _scaleMode = ScaleMode.FitToWidth; // 默认匹配宽度

        public enum ScaleMode
        {
            Stretch, // 拉伸填充
            FitToWidth, // 适应宽度
            FitToHeight, // 适应高度
            FitInside, // 适应内部（不超出）
            FitOutside, // 适应外部（完全填充）
            Null,
        }

        public virtual void SetScale(Vector3 scale, ScaleMode mode = ScaleMode.Null)
        {
            if (SpriteRenderer == null || SpriteRenderer.sprite == null)
            {
                LogManager.Warning(
                    "SpriteRenderer 或 Sprite 为空，无法进行缩放",
                    nameof(PooledObjectBehaviour),
                    false
                );
                return;
            }

            Vector2 spriteSize = SpriteRenderer.sprite.bounds.size;
            Vector3 newScale = CalculateScale(
                spriteSize,
                scale,
                mode == ScaleMode.Null ? _scaleMode : mode
            );

            if (mode != ScaleMode.Null)
            {
                _scaleMode = mode;
            }

            transform.localScale = newScale;
        }

        public void ReScale(Vector2 newScale, ScaleMode mode = ScaleMode.FitToWidth)
        {
            _scaleMode = mode;
            SetScale(newScale);
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

        private void Awake() // 这个不能编译时生成
        {
            RegisterSprites();
        }

        public virtual void RegisterSprites()
        {
            if (SpriteList is not null && _sprites is not null)
            {
                _sprites.Clear();

                foreach (var sprite in SpriteList)
                {
                    if (sprite != null && !string.IsNullOrEmpty(sprite.name))
                    {
                        if (!_sprites.ContainsKey(sprite.name))
                        {
                            _sprites.Add(sprite.name, sprite);
                        }
                        else
                        {
                            LogManager.Warning(
                                $"重复的精灵名称: {sprite.name}，将被跳过。",
                                nameof(PooledObjectBehaviour)
                            );
                        }
                    }
                }
            }
        }

        public virtual Sprite GetSprite(string key)
        {
            return _sprites[key];
        }
    }

    public class PooledObjectBehaviour : ScaleableSpriteBehaviour, IPageControlled
    {
        public UnityEvent DestroyEvent = new(); // 外部调用

        public AnimeMachine AnimeMachine; // 在子类中初始化

        public virtual void OnAwake() { }

        public virtual void OnOpenPage() { }

        public virtual void OnClosePage() { }
    }
}
