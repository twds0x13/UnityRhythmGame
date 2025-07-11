using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //没有指令
    private class Unit_Condition_NoInstruction : Condition<Unit>
    {
        private Unit_Condition_NoInstruction(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(Unit subject)
        {
            return subject._curInstructions == null;
        }
    }
}
