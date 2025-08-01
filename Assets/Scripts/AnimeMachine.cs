using System.Collections.Generic;
using PooledObjectNS;
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
    /// 把动画所需的各种控制器打包成的一个类
    /// 包含一大堆不常用数值和默认值
    /// </summary>
    public class AnimeMachine
    {
        public bool HasDisappearAnime = true;

        public bool HasJudgeAnime = true; // 这个只对 Note 生效

        public bool IsDestroyable = true; // 只代表处于当前界面时无法摧毁，依旧会被退出页面自动销毁

        public float DisappearTimeSpan = 0.2f;

        public float JudgeAnimeTimeSpan = 0.2f;

        public float DisappearTimeCache;

        public Vector3 DisappearingPosCache;

        public float JudgeTimeCache;

        public Vector3 JudgePosCache;

        public float CurT // 用来处理 Interpolation 函数，需要值的范围在 0 ~ 1
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
                AnimeQueue.Enqueue(Item);
            }
        }
    }
}
