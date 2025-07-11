using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = System.Object;

namespace StarryJamFSM
{
    /// <summary>
    /// 状态类
    /// </summary>
    /// <typeparam name="T">主体</typeparam>
    public abstract class State<T> : IState<T>
    {
        public Enum StateID { get; protected set; }
        
        public IStateMachine<T> StateMachine { get; private set; }

        protected State(IStateMachine<T> stateMachine, Enum stateID)
        {
            StateID = stateID;
            StateMachine = stateMachine;
        }

        //状态主体
        public T Subject => StateMachine.Subject;
        
        /// <summary>
        /// 转换条件和对应状态的映射表
        /// </summary>
        private readonly Dictionary<Condition<T>, Enum> _conditionMap = new  Dictionary<Condition<T>, Enum>();

        //状态初始化
        public virtual void Awake() { }
        
        //切入时动作
        public virtual void Enter() { }
        
        //持续行为
        public virtual void Update() { }
        
        //切出时动作
        public virtual void Leave() { }
        
        /// <summary>
        /// 添加转换条件
        /// </summary>
        public IState<T> AddCondition(Condition<T> condition, Enum stateID)
        {
            if (_conditionMap.ContainsKey(condition))
            {
                Debug.LogWarning($"条件{condition}被重复添加，一个条件只能对应一种次态！");
            }
            else
                _conditionMap.Add(condition, stateID);

            return this;
        }

        /// <summary>
        /// 检查是否满足转换条件
        /// </summary>
        public Enum TransitionCheck()
        {
            return (from map in _conditionMap where map.Key.ConditionCheck(Subject) select map.Value).FirstOrDefault();
        }
    }
}
