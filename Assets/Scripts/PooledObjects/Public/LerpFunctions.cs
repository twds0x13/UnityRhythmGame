using System;
using UnityEngine;

namespace InterpNS
{
    public enum AxisFunc
    {
        Linear,
        Sine,
        Cosine,
        Pow,
    }

    /// <summary>
    /// 包含输出从 0 ~ 1 浮点数的函数工具类
    /// Cos 等在 0 ~ <see cref="Mathf.PI"/>/2 定义域上输出 1 ~ 0 的函数取负数
    /// </summary>
    public class InterpFunc
    {
        public static Vector3 VectorHandler(
            Vector3 StartV,
            Vector3 EndV,
            float CurT,
            AxisFunc XFunc,
            AxisFunc YFunc,
            AxisFunc ZFunc,
            float PowX = 1f,
            float PowY = 1f,
            float PowZ = 1f
        )
        {
            return new Vector3(
                FloatHandler(StartV.x, EndV.x, CurT, XFunc, PowX),
                FloatHandler(StartV.y, EndV.y, CurT, YFunc, PowY),
                FloatHandler(StartV.z, EndV.z, CurT, ZFunc, PowZ)
            );
        }

        public static float FloatHandler(
            float Start,
            float End,
            float CurT,
            AxisFunc Func,
            float Pow
        )
        {
            switch (Func)
            {
                case AxisFunc.Linear:
                    return LerpFloat(Start, End, CurT);
                case AxisFunc.Sine:
                    return LerpFloat(Start, End, Mathf.Sin(0.5f * Mathf.PI * CurT));
                case AxisFunc.Cosine:
                    return LerpFloat(Start, End, 1f - 1f * Mathf.Cos(0.5f * Mathf.PI * CurT));
                case AxisFunc.Pow:
                    return LerpFloat(Start, End, Mathf.Pow(CurT, Pow));
                default:
                    throw new InvalidOperationException(Func.ToString() + " Not Found.");
            }
        }

        public static float LerpFloat(float Start, float End, float CurT)
        {
            return (1f - CurT) * Start + CurT * End;
        }
    }
}
