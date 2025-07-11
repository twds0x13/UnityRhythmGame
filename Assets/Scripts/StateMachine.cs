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
}
