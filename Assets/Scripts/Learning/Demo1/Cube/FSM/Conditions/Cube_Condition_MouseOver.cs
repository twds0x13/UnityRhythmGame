using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;


public partial class Cube
{
    //鼠标时候悬浮
    private class Cube_Condition_MouseOver : Condition<Cube>
    {
        public Cube_Condition_MouseOver(Enum conditionID) : base(conditionID) { }
        
        public override bool ConditionCheck(Cube subject)
        {
            return subject.mouseOver;
        }
    }
}
