using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

public partial class Cube
{
    //鼠标是否松开
    private class Cube_Condition_MouseUp : Condition<Cube>
    {
        public Cube_Condition_MouseUp(Enum conditionID) : base(conditionID) { }
        
        public override bool ConditionCheck(Cube subject)
        {
            return !subject.mouseDown;
        }
    }
}
