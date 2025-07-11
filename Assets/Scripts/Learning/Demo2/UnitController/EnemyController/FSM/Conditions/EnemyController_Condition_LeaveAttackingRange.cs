using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class EnemyController
{
    //玩家离开攻击范围
    private class EnemyController_Condition_LeaveAttackingRange : Condition<EnemyController>
    {
        private EnemyController_Condition_LeaveAttackingRange(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(EnemyController subject)
        {
            return subject._GetDistanceWithPlayer() > subject.attackingRange;
        }
    }
}
