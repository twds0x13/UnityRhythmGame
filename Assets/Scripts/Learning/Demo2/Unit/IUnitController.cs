using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位控制器接口，可以获取控制指令
/// </summary>
public interface IUnitController
{
    /// <summary>
    /// 单位控制指令
    /// </summary>
    UnitInstructions Instructions { get; }

}

/// <summary>
/// 默认的控制器类，提供空指令
/// </summary>
 class UnitDefaultController : IUnitController
 {
     public UnitInstructions Instructions => null;
 }
