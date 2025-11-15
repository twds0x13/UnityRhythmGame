using UnityEngine;

// 确保 LineRenderer 组件存在于同一个 GameObject 上
[RequireComponent(typeof(LineRenderer))]
public class HoldBodyAnimator : MonoBehaviour
{
    // === 外部设置 ===

    // 你希望连接的两个点（请在 Inspector 拖入场景中的 Transform）
    public Transform pointA;
    public Transform pointB;

    // 连接线（方块）的宽度
    public float lineWidth = 0.5f;

    // Sprite 贴图的UV长度，即贴图在世界空间中多长才算“一次平铺”
    // 假设你的 Sprite 纹理的宽度/高度是 1 个世界单位
    public float spriteUVLength = 1.0f;

    // === 内部组件 ===
    public LineRenderer lineRenderer;

    void Start()
    {
        // 确保获取到 LineRenderer 组件
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // 设置线条的宽度（确保线条是均匀的“方块”）
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    void Update()
    {
        if (pointA == null || pointB == null)
            return;

        // 1. 设置世界坐标点
        Vector3 posA = pointA.position;
        Vector3 posB = pointB.position;

        lineRenderer.SetPosition(0, posA);
        lineRenderer.SetPosition(1, posB);

        // 2. 计算距离
        float distance = Vector3.Distance(posA, posB);

        // 3. 动态计算贴图平铺次数 (Tile Count)
        // 贴图平铺次数 = 总距离 / (贴图在世界空间中的单位长度)
        float tileCount = distance / spriteUVLength;

        // 4. 更新 LineRenderer 的 Tiling 属性
        // LineRenderer 的 Tiling 对应 Material 的主纹理 Tiling
        if (lineRenderer.material != null)
        {
            // 访问材质，修改主纹理的Tiling Y 值
            // Tiling.x 控制宽度方向的平铺，Tiling.y 控制长度方向的平铺
            lineRenderer.material.mainTextureScale = new Vector2(1f, tileCount);
        }
    }
}
