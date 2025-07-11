using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //移动->攻击
    private class Unit_Condition_Moving2Attacking : Condition<Unit>
    {
        private Unit_Condition_Moving2Attacking(Enum conditionID) : base(conditionID) { }

        public override bool ConditionCheck(Unit subject)
        {
            //一旦有攻击指令优先执行攻击指令
            return subject._curInstructions.attackDirection != Vector2.zero;
        }
    }
}
