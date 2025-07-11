using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSMIDRuleConfig
{
    //单个主体的状态、条件的预留数量
    public const int IDLimit = 1000;
    
    public enum SubjectType
    {
        None,
        Cube,
        Unit,
        EnemyController,
    }
}
