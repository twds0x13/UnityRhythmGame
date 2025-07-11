using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;


partial class EnemyController
{
    private class EnemyController_State_Attacking : State<EnemyController>
    {
        private EnemyController_State_Attacking(IStateMachine<EnemyController> stateMachine, Enum stateID) : base(stateMachine, stateID) { }

        public override void Update()
        {
            base.Update();
            var instruction = new UnitInstructions();
            instruction.attackDirection = (Subject._player.transform.position - Subject.transform.position).ToVector2();
            Subject.Instructions = instruction;
        }
    }
}
