using GBehaviour;
using NBehaviour;
using UnityEditor.Build.Content;
using UnityEngine;

namespace IUtils
{
    public interface IVector3
    {
        public Vector3 StartV { get; set; } // 起点 Vector3 向量

        public Vector3 EndV { get; set; } // 终点 Vector3 向量

        public abstract float StartT { get; set; } // 动画开始时间

        public abstract float EndT { get; set; } // 动画结束时间

        public abstract bool IsFinished { get; set; } // ...
    }

    public interface IDev
    {
        public abstract void DevLog();
    }

    public interface ITime
    {
        public abstract float JudgeTime(); // 经过用户设置后的判定时间戳

        public abstract float AbsTime(); // 包含游戏加载时间的绝对时间戳

        public abstract void Pause(); // ...

        public abstract void Resume(); // ...
    }
}
