using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //攻击->移动条件
    private class Unit_Condition_Attacking2Moving : Condition<Unit>
    {
        private Unit_Condition_Attacking2Moving(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(Unit subject)
        {
            return
                subject._curInstructions.attackDirection == Vector2.zero &&
                subject._curInstructions.moveDirection != Vector2.zero;
        }
    }
}
