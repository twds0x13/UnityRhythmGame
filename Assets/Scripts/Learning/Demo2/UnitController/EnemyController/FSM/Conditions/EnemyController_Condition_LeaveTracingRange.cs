using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class EnemyController
{
    
    private class EnemyController_Condition_LeaveTracingRange : Condition<EnemyController>
    {
        private EnemyController_Condition_LeaveTracingRange(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(EnemyController subject)
        {
            return subject._GetDistanceWithPlayer() > subject.tracingRange;
        }
    }
}
