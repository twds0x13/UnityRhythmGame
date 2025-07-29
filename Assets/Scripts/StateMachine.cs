using System;
using System.Collections.Generic;
using Anime;
using NoteNamespace;
using PooledObject;
using UnityEngine.Windows.Speech;

namespace StateMachine
{
    public interface IState<T>
    {
        void Enter();
        void Update();
        void Exit();
    }

    /// <summary>
    /// ͨ��״̬������棬��Ҫ�Լ�����ÿһ��״̬��Ȼ����״̬����д��ת�߼��������SwitchState��ת
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StateMachine<T>
    {
        public IState<T> CurState;

        public void InitState(IState<T> State)
        {
            CurState = State;
            CurState?.Enter();
        }

        public void SwitchState(IState<T> State)
        {
            CurState?.Exit();
            CurState = State;
            CurState?.Enter();
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseState<T> : IState<T>
        where T : PooledObjectBehaviour
    {
        protected StateMachine<T> StateMachine;

        protected AnimeMachine AnimeMachine;

        protected T Self;

        public BaseState(T Self, StateMachine<T> StateMachine)
        {
            this.Self = Self;
            this.StateMachine = StateMachine;
            this.AnimeMachine = Self.Instance.AnimeMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }
}
