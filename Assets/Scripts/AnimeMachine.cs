using System.Collections.Generic;
using PooledObject;
using UnityEngine;

namespace Anime
{
    /// <summary>
    /// 动画切片结构体，保存一段时间内 <see cref="PooledObjectBehaviour"/> 对象的位移数据
    /// </summary>
    public struct AnimeClip
    {
        public Vector3 StartV { get; set; }

        public Vector3 EndV { get; set; }

        public float StartT { get; set; }

        public float EndT { get; set; }

        public AnimeClip(
            float StartT = 0f,
            float EndT = 0f,
            Vector3 StartV = default,
            Vector3 EndV = default
        )
        {
            this.StartV = StartV;

            this.EndV = EndV;

            this.StartT = StartT;

            this.EndT = EndT;
        }

        public float TotalTimeElapse()
        {
            return EndT - StartT;
        }
    }

    /// <summary>
    /// 把动画所需的所有控制器打包成的一个类
    /// TODO : 尝试用 <see cref="ScriptableObject"/> 重构
    /// </summary>
    public class AnimeMachine
    {
        public bool HasDisappearAnime = true;

        public bool IsDestroyable = true;

        public float DisappearTimeSpan = 0.2f;

        public float DisappearTimeCache;

        public float DisappearCurTCache;

        public Vector3 DisappearingPosCache;

        public float CurT // 用来处理 Lerp 函数，需要值的范围在 0 ~ 1 间往复循环。在到达 1 时跳转回 0
        {
            get { return _t; }
            set { _t = Mathf.Clamp01(value); }
        }

        private float _t;

        public Queue<AnimeClip> AnimeQueue = new();

        public AnimeClip CurAnime;

        public AnimeMachine(Queue<AnimeClip> Queue)
        {
            foreach (var Item in Queue)
            {
                this.AnimeQueue.Enqueue(Item);
            }
        }
    }
}
