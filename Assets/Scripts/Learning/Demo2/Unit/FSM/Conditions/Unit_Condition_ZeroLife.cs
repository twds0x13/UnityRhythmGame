using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //生命值小于等于零
    private class Unit_Condition_ZeroLife : Condition<Unit>
    {
        private Unit_Condition_ZeroLife(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(Unit subject)
        {
            return subject.curLife <= 0;
        }
    }
}
