using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //存活（复合）状态
    private class Unit_State_Alive : CompositeState<Unit>
    {
        public Unit_State_Alive(IStateMachine<Unit> stateMachine, Enum stateID) : base(stateMachine, stateID) { }
        
        public override void Update()
        {
            base.Update();
            //每帧从控制器获取命令
            Subject._curInstructions = Subject.Controller.Instructions;
        }
        
    }
}
