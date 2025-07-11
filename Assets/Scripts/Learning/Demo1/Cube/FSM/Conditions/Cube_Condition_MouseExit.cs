using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

public partial class Cube
{
    //鼠标是否移开
    private class Cube_Condition_MouseExit : Condition<Cube>
    {
        public Cube_Condition_MouseExit(Enum conditionID) : base(conditionID) { }
        
        public override bool ConditionCheck(Cube subject)
        {
            return !subject.mouseOver;
        }

    }
}
