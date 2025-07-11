using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

namespace StarryJamFSM
{
    /// <summary>
    /// 复合状态类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract  class CompositeState<T> : State<T>, IStateMachine<T>
    {
        protected CompositeState(IStateMachine<T> stateMachine, Enum stateID) : base(stateMachine, stateID)
        {
            _innerStateMachine = new FiniteStateMachine<T>(Subject);
        }
        
        private readonly FiniteStateMachine<T> _innerStateMachine;

        public Enum DefaultStateID
        {
            get => _innerStateMachine.DefaultStateID;
            set => _innerStateMachine.DefaultStateID = value;
        }

        public override void Awake()
        {
            base.Awake();
            _innerStateMachine.Awake();
        }

        public override void Enter()
        {
            base.Enter();
            _innerStateMachine.Start();
        }

        public override void Update()
        {
            base.Update();
            _innerStateMachine.Update();
        }

        public override void Leave()
        {
            base.Leave();
            _innerStateMachine.ChangeState(null);
        }

        public IStateMachine<T> AddState(Enum state)
        {
            _innerStateMachine.AddState(state);

            return this;
        }

        public IStateMachine<T> AddTransition(Enum fromState, Enum toState, Enum condition)
        {
            _innerStateMachine.AddTransition(fromState, toState, condition);

            return this;
        }

        public IStateMachine<T> Open(Enum stateID)
        {
            return _innerStateMachine.Open(stateID);
        }

        public void ChangeState(Enum targetState)
        {
            _innerStateMachine.ChangeState(targetState);
        }
    }

}