using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;

/// <summary>
/// 单位指令
/// </summary>
public class UnitInstructions
{
    /// <summary>
    /// 移动方向（归一化的）
    /// </summary>
    public Vector2 moveDirection
    {
        get => _moveDirection;
        set => _moveDirection = value.normalized;
    }

    private Vector2 _moveDirection;

    /// <summary>
    /// 攻击方向（归一化的）
    /// </summary>
    public Vector2 attackDirection
    {
        get => _attackDirection;
        set => _attackDirection = value.normalized;
    }

    private Vector2 _attackDirection;
}
