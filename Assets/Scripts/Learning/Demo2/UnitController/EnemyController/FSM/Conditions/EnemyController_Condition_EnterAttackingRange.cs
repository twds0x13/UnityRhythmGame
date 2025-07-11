using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class EnemyController
{
    //玩家在攻击范围内
    private class EnemyController_Condition_EnterAttackingRange : Condition<EnemyController>
    {
        public EnemyController_Condition_EnterAttackingRange(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(EnemyController subject)
        {
            return subject._GetDistanceWithPlayer() <= subject.attackingRange;
        }
    }
}
