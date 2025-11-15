using System.Collections.Generic;
using PooledObjectNS;
using UnityEngine;
using Vector3ExtensionsNS;

namespace Anime
{
    public static class Defaults // 应该用Scriptable Object来存储这些默认值，但现在先这样吧
    {
        public const bool HasDisappearAnime = true;

        public const bool HasJudgeAnime = true;

        public const bool IsDestroyable = true;

        public const float DisappearTimeSpan = 0.2f;

        public const float JudgeAnimeTimeSpan = 0.2f;
    }

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

        public readonly float TotalTimeElapse => EndT - StartT;
    }

    /// <summary>
    /// 把动画所需的各种控制器打包成的一个类
    /// </summary>
    public class AnimeMachine
    {
        public bool HasDisappearAnime = Defaults.HasDisappearAnime;

        public bool HasJudgeAnime = Defaults.HasJudgeAnime;

        public bool IsDestroyable = Defaults.IsDestroyable; // 只代表处于当前界面时无法摧毁，依旧会被退出页面自动销毁

        public float DisappearTimeSpan = Defaults.DisappearTimeSpan;

        public float JudgeAnimeTimeSpan = Defaults.JudgeAnimeTimeSpan;

        public float DisappearTimeCache;

        public Vector3 DisappearingPosCache;

        public float JudgeTimeCache;

        public Vector3 JudgePosCache;

        public float CurT // 用来处理 Lerp 函数，需要值的范围在 0 ~ 1
        {
            get { return _t; }
            set { _t = Mathf.Clamp01(value); }
        }

        private float _t;

        public Queue<AnimeClip> AnimeQueue = new();

        public AnimeClip CurAnime;

        public AnimeMachine(Queue<AnimeClip> Queue, float offset = 0f)
        {
            foreach (var item in Queue)
            {
                var tmp = item; // 复制迭代器，避免引用问题

                if (offset != 0f)
                {
                    tmp.StartT += offset;
                    tmp.EndT += offset;
                }

                AnimeQueue.Enqueue(tmp);
            }
        }
    }

    public static class AnimeMachineExtensions
    {
        public static AnimeMachine ResetOffset(this AnimeMachine machine, float offset)
        {
            var newAnimeMachine = new AnimeMachine(machine.AnimeQueue, offset);

            return newAnimeMachine;
        }
    }
}
