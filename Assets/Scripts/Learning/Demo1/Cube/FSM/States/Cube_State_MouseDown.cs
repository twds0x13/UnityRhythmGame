using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

public partial class Cube
{
    //鼠标按下状态
    private class Cube_State_MouseDown : State<Cube>
    {
        private Cube_State_MouseDown(FiniteStateMachine<Cube> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Enter()
        {
            base.Enter();
            Subject.material.color = Color.red;
        }
    }
}
