using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace StarryJamFSM
{
    public enum Cube_State
    {
        None = FSMIDRuleConfig.SubjectType.Cube * FSMIDRuleConfig.IDLimit,
    
        /// <summary>
        /// 默认状态
        /// </summary>
        Default,
        /// <summary>
        /// 鼠标悬浮状态
        /// </summary>
        MouseOver,
        /// <summary>
        /// 鼠标按下状态
        /// </summary>
        MouseDown,
    }

    public enum Unit_State
    {
        None = FSMIDRuleConfig.SubjectType.Unit * FSMIDRuleConfig.IDLimit,
    
        /// <summary>
        /// 存活（复合）
        /// </summary>
        Alive,
        /// <summary>
        /// 死亡
        /// </summary>
        Dead,
        /// <summary>
        /// 待机
        /// </summary>
        Idle,
        /// <summary>
        /// 执行指令
        /// </summary>
        Executing,
        /// <summary>
        /// 攻击
        /// </summary>
        Attacking,
        /// <summary>
        /// 移动
        /// </summary>
        Moving
    }

    public enum EnemyController_State
    {
        None = FSMIDRuleConfig.SubjectType.EnemyController * FSMIDRuleConfig.IDLimit,
        
        /// <summary>
        /// 待机状态
        /// </summary>
        Idle,
        
        /// <summary>
        /// 追击状态
        /// </summary>
        Tracing,
        
        /// <summary>
        /// 攻击状态
        /// </summary>
        Attacking,
    }
}

