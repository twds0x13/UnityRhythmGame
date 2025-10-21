using System.Collections.Generic;

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

        public virtual void InitState(IState<T> State)
        {
            CurState = State;
            CurState?.Enter();
        }

        public virtual void SwitchState(IState<T> State)
        {
            if (CurState != State)
            {
                CurState?.Exit();
                CurState = State;
                CurState?.Enter();
            }
        }
    }

    public class LinearStateMachine<T> : StateMachine<T>
    {
        public List<IState<T>> States { get; private set; }

        private int curStateIndex = -1;

        /// <summary>
        /// 使用状态列表初始化线性状态机
        /// </summary>
        /// <param name="states">状态列表，最后一个状态应包含销毁逻辑</param>
        public void InitLinear(List<IState<T>> states)
        {
            States = states;
            if (States != null && States.Count > 0)
            {
                curStateIndex = 0;
                InitState(States[0]);
            }
        }

        /// <summary>
        /// 安全地进入下一个状态
        /// </summary>
        /// <returns>是否成功切换到下一个状态</returns>
        public bool NextState()
        {
            if (States == null || curStateIndex < 0 || curStateIndex >= States.Count - 1)
            {
                // 已处于最后一个状态或状态列表无效
                return false;
            }

            curStateIndex++;
            SwitchState(States[curStateIndex]);
            return true;
        }

        /// <summary>
        /// 获取当前状态的索引
        /// </summary>
        public int Index => curStateIndex;

        /// <summary>
        /// 检查是否处于最后一个状态
        /// </summary>
        public bool IsLastState => curStateIndex == States.Count - 1;
    }

    /// <summary>
    /// 统一 Note 和 Track 基本状态的尝试，暂时用不到
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /*
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
    */
}
