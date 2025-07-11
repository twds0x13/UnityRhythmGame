using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class EnemyController
{
    //玩家进入追击范围
    private class EnemyController_Condition_EnterTracingRange : Condition<EnemyController>
    {
        private EnemyController_Condition_EnterTracingRange(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(EnemyController subject)
        {
            return subject._GetDistanceWithPlayer() <= subject.tracingRange;
        }
    }
}
