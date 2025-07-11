using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

public partial class Cube
{
    //默认状态
    private class Cube_State_Default : State<Cube>
    {
        private Cube_State_Default(FiniteStateMachine<Cube> stateMachine, Enum stateID) : base(stateMachine, stateID) { }
    
        public override void Enter()
        {
            base.Enter();
            Subject.material.color = Color.white;
        }

    }
}
