using System;
using System.Collections.Generic;
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
}
