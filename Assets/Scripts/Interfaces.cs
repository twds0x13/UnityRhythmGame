using UnityEngine;

/// <summary>
/// 主要用途是给人看注释...
/// </summary>
namespace IUtils
{
    public interface IAnime
    {
        public Vector3 StartV { get; set; } // 起点 Vector3 向量

        public Vector3 EndV { get; set; } // 终点 Vector3 向量

        public abstract float StartT { get; set; } // 动画开始时间

        public abstract float EndT { get; set; } // 动画结束时间
    }

    /// <summary>
    /// 输出开发者日志的对象需要的接口
    /// </summary>
    public interface IDev
    {
        public abstract void DevLog(); // 输出开发者日志
    }

    /// <summary>
    /// 使用ObjectPool的游戏物体所需满足的接口
    /// </summary>
    public interface IPooling
    {
        public abstract void HandlerManager();
    }

    /// <summary>
    /// 输出时间戳的对象需要的接口
    /// </summary>
    public interface IGameBehaviour
    {
        public abstract float GetGameTime(); // 经过用户设置后的判定时间戳

        public abstract float GetAbsTime(); // 包含游戏加载时间的绝对时间戳
    }
}
