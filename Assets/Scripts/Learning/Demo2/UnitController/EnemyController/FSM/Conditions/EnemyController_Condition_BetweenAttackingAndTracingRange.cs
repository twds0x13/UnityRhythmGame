using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class EnemyController
{
    //玩家在攻击范围和追击范围之间
    private class EnemyController_Condition_BetweenAttackingAndTracingRange : Condition<EnemyController>
    {
        private EnemyController_Condition_BetweenAttackingAndTracingRange(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(EnemyController subject)
        {
            return subject._GetDistanceWithPlayer() <= subject.tracingRange &&
                   subject._GetDistanceWithPlayer() >= subject.attackingRange;
        }
    }
}
