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
    public readonly struct AnimeClip
    {
        public readonly Vector3 StartV { get; }

        public readonly Vector3 EndV { get; }

        public readonly float StartT { get; }

        public readonly float EndT { get; }

        public readonly float TotalTimeElapse => EndT - StartT;

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

        public AnimeClip Offset(float offset = 0f) =>
            new(StartT + offset, EndT + offset, StartV, EndV);
    }

    public readonly struct Pack
    {
        public readonly Vector3 Vector;

        public readonly float Time;

        /// <summary>
        /// 打包缓存组，避免单独修改 Cache 内容
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="time"></param>
        public Pack(Vector3 vector, float time)
        {
            Vector = vector;
            Time = time;
        }
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

        public float DisappearTimeCache => DisappearCache.Time;

        public Vector3 DisappearingPosCache => DisappearCache.Vector;

        public float JudgeTimeCache => JudgeCache.Time;

        public Vector3 JudgePosCache => JudgeCache.Vector;

        public Pack DisappearCache;

        public Pack JudgeCache;

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
                var tmp = item.Offset(offset);

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
