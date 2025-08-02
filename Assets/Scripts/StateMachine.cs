using System;
using System.Collections.Generic;
using Anime;
using NoteNS;
using PooledObjectNS;
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
    /// 通用状态机极简版，需要自己定义每一个状态，然后在状态里面写跳转逻辑，最后用SwitchState跳转
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StateMachine<T>
    {
        public IState<T> CurState { get; private set; }

        public void InitState(IState<T> State)
        {
            CurState = State;
            CurState?.Enter();
        }

        public void SwitchState(IState<T> State)
        {
            if (CurState != State)
            {
                CurState?.Exit();
                CurState = State;
                CurState?.Enter();
            }
        }
    }

    /// <summary>
    /// 统一 Note 和 Track 基本状态的尝试，暂时用不到
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
            this.AnimeMachine = Self.Inst.AnimeMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }
}
