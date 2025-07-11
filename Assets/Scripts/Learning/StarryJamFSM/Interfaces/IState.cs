using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarryJamFSM
{
    /// <summary>
    /// 状态接口
    /// </summary>
    /// <typeparam name="T">主体类</typeparam>
    public interface IState<T>
    {
        /// <summary>
        /// 状态ID
        /// </summary>
        Enum StateID { get; }
        
        /// <summary>
        /// 状态主体
        /// </summary>
        T Subject { get; }
        
        /// <summary>
        /// 从属的状态机
        /// </summary>
        IStateMachine<T> StateMachine { get; }

        /// <summary>
        /// 状态初始化
        /// </summary>
        void Awake();

        /// <summary>
        /// 切入动作
        /// </summary>
        void Enter();

        /// <summary>
        /// 持续动作
        /// </summary>
        void Update();

        /// <summary>
        /// 切出动作
        /// </summary>
        void Leave();

        /// <summary>
        /// 添加转换条件
        /// </summary>
        /// <param name="conditionID">条件ID</param>
        /// <param name="stateID">目标状态ID</param>
        /// <returns>this</returns>
        IState<T> AddCondition(Condition<T> conditionID, Enum stateID);

        /// <summary>
        /// 转换条件检查
        /// </summary>
        /// <returns>第一个满足条件的目标状态ID，没有则为null</returns>
        Enum TransitionCheck();
    }
}
