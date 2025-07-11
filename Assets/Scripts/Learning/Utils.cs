using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

/// <summary>
/// 工具方法集
/// </summary>
public static class Utils
{
    public static Vector3 ToVector3(this Vector2 vector2)
    {
        return new Vector3(vector2.x, 0, vector2.y);
    }

    public static Vector2 ToVector2(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }
}
