using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

public partial class Cube
{
    //鼠标是否按下
    private class Cube_Condition_MouseDown : Condition<Cube>
    {
        private Cube_Condition_MouseDown(Enum conditionID) : base(conditionID) { }
        
        public override bool ConditionCheck(Cube subject)
        {
            return subject.mouseDown;
        }
    }
}
