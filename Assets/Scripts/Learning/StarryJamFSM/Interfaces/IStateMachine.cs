using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarryJamFSM
{
    /// <summary>
    /// 状态机接口
    /// </summary>
    /// <typeparam name="T">主体类</typeparam>
    public interface IStateMachine<T>
    {
        /// <summary>
        /// 状态主体
        /// </summary>
        T Subject { get; }
    
        /// <summary>
        /// 默认状态
        /// </summary>
        Enum DefaultStateID { get; set; }

        /// <summary>
        /// 添加状态（建议直接使用添加转换关系方法AddTransition()）
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        IStateMachine<T> AddState(Enum state);

        /// <summary>
        /// 添加状态转换关系，如果所加转换中有尚未添加的状态会自动添加
        /// </summary>
        /// <param name="fromState">来源状态ID</param>
        /// <param name="toState">目标状态ID</param>
        /// <param name="condition">条件ID</param>
        /// <returns>this</returns>
        IStateMachine<T> AddTransition(Enum fromState, Enum toState, Enum condition);

        /// <summary>
        /// 打开复合状态的子状态机
        /// </summary>
        /// <param name="stateID">复合状态ID</param>
        /// <returns>子状态机</returns>
        IStateMachine<T> Open(Enum stateID);

        /// <summary>
        /// 变更状态
        /// </summary>
        /// <param name="targetState">目标状态ID</param>
        void ChangeState(Enum targetState);
    }
}
