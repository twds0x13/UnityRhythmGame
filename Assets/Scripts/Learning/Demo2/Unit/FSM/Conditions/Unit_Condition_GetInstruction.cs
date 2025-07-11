using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //收到新的指令
    private class Unit_Condition_GetInstruction : Condition<Unit>
    {
        private Unit_Condition_GetInstruction(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(Unit subject)
        {
            return subject._curInstructions != null;
        }
    }
}
