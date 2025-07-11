using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //执行指令（复合）状态
    private class Unit_State_Executing : CompositeState<Unit>
    {
        private Unit_State_Executing(IStateMachine<Unit> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Awake()
        {
            base.Awake();
            
            //考虑到未来很可能会加入新的指令状态，将子状态的配置封装在当前类当中了
            AddTransition(Unit_State.Attacking, Unit_State.Moving, Unit_Condition.Attacking2Moving);
            AddTransition(Unit_State.Moving, Unit_State.Attacking, Unit_Condition.Moving2Attacking);
            
            //只是为了可以成功初始化所以设置一个初始状态
            DefaultStateID = Unit_State.Moving;
        }

        public override void Enter()
        {
            //根据指令优先级决定默认状态
            if(Subject._curInstructions.attackDirection != Vector2.zero)
                DefaultStateID = Unit_State.Attacking;
            else
                DefaultStateID = Unit_State.Moving;
            
            base.Enter();
        }
    }
}
