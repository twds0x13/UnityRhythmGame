using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

public partial class Cube
{
    //鼠标悬浮状态
    private class Cube_State_MouseOver : State<Cube>
    {
        public Cube_State_MouseOver(FiniteStateMachine<Cube> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Enter()
        {
            base.Enter();
            Subject.material.color = Color.yellow;
        }
    }
}
