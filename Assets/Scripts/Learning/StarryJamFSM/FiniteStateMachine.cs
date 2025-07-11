using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM.Factory;
using UnityEngine;
using UnityEngineInternal;

namespace StarryJamFSM
{
    /// <summary>
    /// 状态机类
    /// </summary>
    /// <typeparam name="T">主体</typeparam>
    public class FiniteStateMachine<T> : IStateMachine<T>
    {
        public FiniteStateMachine(T subject)
        {
            Subject = subject;
        }
        
        public Enum DefaultStateID { get; set; }

        public T Subject { get; private set; }
    
        private List<IState<T>> _states = new List<IState<T>>();
        
        private IState<T> _currentState;

        public void Awake()
        {
            foreach (var state in _states)
            {
                state.Awake();
            }
        }

        public void Start()
        {
            if (DefaultStateID == null)
            {
                Debug.LogError("状态机没有设置默认状态");
                return;
            }

            ChangeState(DefaultStateID);
        }

        public void Update()
        {
            var targetState = _currentState.TransitionCheck();
            if (targetState != null)
            {
                ChangeState(targetState);
            } 
            
            _currentState.Update();
        }
        
        public IStateMachine<T> AddState(Enum stateID)
        {
            if (_states.Find(state => state.StateID == stateID) != null)
                Debug.LogWarning($"尝试重复添加状态{stateID}");
            else
            {
                var addedState = StateFactory.GetState<T>(this, stateID);
                _states.Add(addedState);   
            }
            
            return this;
        }
        
        public IStateMachine<T> AddTransition(Enum fromState, Enum toState, Enum condition)
        {
            var fState = _GetState(fromState);
            var tState = _GetState(toState);
            if (fState == null)
            {
                AddState(fromState);
                fState = _GetState(fromState);
            }

            if (tState == null)
            {
                AddState(toState);
                tState = _GetState(toState);
            }

            var addCondition = ConditionFactory.GetCondition<T>(condition);
            fState.AddCondition(addCondition, toState);

            return this;
        }

        public IStateMachine<T> Open(Enum stateID)
        {
            var state = _GetState(stateID);
            try
            {
                var compState = (CompositeState<T>)state;

                return compState;
            }
            catch (Exception e)
            {
                Debug.LogError($"获取复合状态错误，ID：{stateID}");
                Debug.LogError($"错误类型：{e}");
                throw;
            }
        }

        //切出当前状态，切入目标状态
        public void ChangeState(Enum targetStateID)
        {
            //触发现态切出动作
            _currentState?.Leave();
            
            //传入空值表示仅退出当前状态
            if (targetStateID == null)
            {
                _currentState = null;
                return;
            }

            //触发次态切入动作
            var targetState = _GetState(targetStateID);
            targetState.Enter();
            //将次态设置为现态
            _currentState = targetState;
        }

        private IState<T> _GetState(Enum stateID)
        {
            IState<T> ret = _states.Find(state => state.StateID.Equals(stateID));
            return ret;
        }
    }

}