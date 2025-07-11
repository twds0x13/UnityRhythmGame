using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarryJamFSM
{
    public enum Cube_Condition
    {
        None = FSMIDRuleConfig.SubjectType.Cube * FSMIDRuleConfig.IDLimit,

        /// <summary>
        /// 鼠标是否悬浮
        /// </summary>
        MouseOver,

        /// <summary>
        /// 鼠标是否移除
        /// </summary>
        MouseExit,

        /// <summary>
        /// 鼠标是否按下
        /// </summary>
        MouseDown,

        /// <summary>
        /// 鼠标是否弹起
        /// </summary>
        MouseUp,
    }

    public enum Unit_Condition
    {
        None = FSMIDRuleConfig.SubjectType.Unit * FSMIDRuleConfig.IDLimit,

        /// <summary>
        /// 攻击->移动
        /// </summary>
        Attacking2Moving,

        /// <summary>
        /// 移动->攻击
        /// </summary>
        Moving2Attacking,

        /// <summary>
        /// 有新指令
        /// </summary>
        GetInstruction,
        
        /// <summary>
        /// 没有指令
        /// </summary>
        NoInstruction,

        /// <summary>
        /// 生命为0
        /// </summary>
        ZeroLife,
    }

    public enum EnemyController_Condition
    {
        None = FSMIDRuleConfig.SubjectType.EnemyController * FSMIDRuleConfig.IDLimit,
        
        /// <summary>
        /// 玩家进入追击距离
        /// </summary>
        EnterTracingRange,
        
        
        /// <summary>
        /// 玩家离开追击距离
        /// </summary>
        LeaveTracingRange,
        
        /// <summary>
        /// 玩家进入攻击距离
        /// </summary>
        EnterAttackingRange,
        
        /// <summary>
        /// 玩家离开攻击距离
        /// </summary>
        LeaveAttackingRange,
        
        /// <summary>
        /// 玩家处于攻击距离和追击距离之间
        /// </summary>
        BetweenAttackingAndTracingRange,
    }
}
