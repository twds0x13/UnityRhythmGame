using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //死亡状态
    private class Unit_State_Dead : State<Unit>
    {
        private Unit_State_Dead(IStateMachine<Unit> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Enter()
        {
            base.Enter();
            //为了更明显一些将材质变为红色
            Subject.GetComponent<MeshRenderer>().material.color = Color.red;
            //改变状态文本
            Subject._stateTxt = "Dead";
        }
    }
}
