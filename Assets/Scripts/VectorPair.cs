using IUtils;
using UnityEngine;

namespace Vectors
{
    public struct VectorPair : IVector3, IDev
    {
        public Vector3 StartV { get; set; }

        public Vector3 EndV { get; set; }

        public float StartT { get; set; }

        public float EndT { get; set; }

        public bool IsFinished { get; set; }

        public VectorPair(
            Vector3 StartV = default,
            Vector3 EndV = default,
            float StartT = 0f,
            float EndT = 0f,
            bool IsFinished = false
        )
        {
            this.StartV = StartV;

            this.EndV = EndV;

            this.StartT = StartT;

            this.EndT = EndT;

            this.IsFinished = IsFinished;
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
    }
}
