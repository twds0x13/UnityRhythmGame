using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;


partial class EnemyController
{
    private class EnemyController_State_Idle : State<EnemyController>
    {
        private EnemyController_State_Idle(IStateMachine<EnemyController> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Enter()
        {
            base.Enter();
            Subject.Instructions = null;
        }
    }
}
