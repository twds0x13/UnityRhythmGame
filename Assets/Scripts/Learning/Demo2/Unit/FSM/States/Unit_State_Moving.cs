using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;


partial class Unit
{
    //移动状态
    private class Unit_State_Moving : State<Unit>
    {
        private Unit_State_Moving(IStateMachine<Unit> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Enter()
        {
            base.Enter();
            //改变状态文本
            Subject._stateTxt = "Moving";
        }

        public override void Update()
        {
            base.Update();
            //根据移动指令的方向进行移动
            Subject.transform.position += Subject._curInstructions.moveDirection.ToVector3() * (Subject.moveSpeed * Time.deltaTime);
        }
    }
}