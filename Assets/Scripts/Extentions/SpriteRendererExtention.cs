using UnityEngine;

public static class SpriteRendererExtension
{
    /// <summary>
    /// 设置 SpriteRenderer 颜色的透明度而不改变 RGB 值
    /// </summary>
    /// <param name="spriteRenderer">目标 SpriteRenderer</param>
    /// <param name="alpha">透明度值 (0-1)</param>
    public static void SetAlpha(this SpriteRenderer spriteRenderer, float alpha)
    {
        if (spriteRenderer == null)
        {
            LogManager.Warning(
                "SpriteRenderer 为空，无法设置透明度",
                nameof(SpriteRendererExtension)
            );
            return;
        }

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    /// <summary>
    /// 获取 SpriteRenderer 颜色的当前透明度
    /// </summary>
    /// <param name="spriteRenderer">目标 SpriteRenderer</param>
    /// <returns>当前透明度值 (0-1)</returns>
    public static float GetAlpha(this SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
        {
            LogManager.Warning(
                "SpriteRenderer 为空，返回默认透明度 1",
                nameof(SpriteRendererExtension)
            );
            return 1f;
        }

        return spriteRenderer.color.a;
    }

    /// <summary>
    /// 淡入 SpriteRenderer（逐渐增加透明度）
    /// </summary>
    /// <param name="spriteRenderer">目标 SpriteRenderer</param>
    /// <param name="duration">淡入持续时间（秒）</param>
    /// <param name="targetAlpha">目标透明度（默认1）</param>
    /// <returns>协程枚举器</returns>
    public static System.Collections.IEnumerator FadeIn(
        this SpriteRenderer spriteRenderer,
        float duration,
        float targetAlpha = 1f
    )
    {
        float startAlpha = spriteRenderer.GetAlpha();
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            spriteRenderer.SetAlpha(newAlpha);
            yield return null;
        }

        spriteRenderer.SetAlpha(targetAlpha);
    }

    /// <summary>
    /// 淡出 SpriteRenderer（逐渐减少透明度）
    /// </summary>
    /// <param name="spriteRenderer">目标 SpriteRenderer</param>
    /// <param name="duration">淡出持续时间（秒）</param>
    /// <param name="targetAlpha">目标透明度（默认0）</param>
    /// <returns>协程枚举器</returns>
    public static System.Collections.IEnumerator FadeOut(
        this SpriteRenderer spriteRenderer,
        float duration,
        float targetAlpha = 0f
    )
    {
        float startAlpha = spriteRenderer.GetAlpha();
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            spriteRenderer.SetAlpha(newAlpha);
            yield return null;
        }

        spriteRenderer.SetAlpha(targetAlpha);
    }
}
