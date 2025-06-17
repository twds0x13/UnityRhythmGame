using IUtils;
using NBehaviour;
using UnityEngine;

namespace Anime
{
    /// <summary>
    /// 动画切片结构体，保存一段时间内<see cref="NoteBehaviour"/>的位移数据
    /// </summary>
    public struct AnimeClip : IAnime, IDev
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

        public void DevLog()
        {
            Debug.LogFormat(
                "Current Anime : StartV: {0} EndV: {1} StartT: {2} EndT: {3}",
                StartV.ToString(),
                EndV.ToString(),
                StartT,
                EndT
            );
        }

        public float TotalTimeElapse()
        {
            return EndT - StartT;
        }
    }
}
