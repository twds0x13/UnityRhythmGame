using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;


partial class Unit
{
    //待机状态，不需要做什么
    private class Unit_State_Idle : State<Unit>
    {
        private Unit_State_Idle(IStateMachine<Unit> stateMachine, Enum stateID) : base(stateMachine, stateID) {}

        public override void Enter()
        {
            base.Enter();
            //改变状态文本
            Subject._stateTxt = "Idle";
        }
    }
}
