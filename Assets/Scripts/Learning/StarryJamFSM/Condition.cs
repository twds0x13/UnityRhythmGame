using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarryJamFSM
{
    /// <summary>
    /// 条件类
    /// </summary>
    /// <typeparam name="T">主体</typeparam>
    public abstract class Condition<T>
    {
        public Enum ConditionID { get; protected set; }

        public Condition(Enum conditionID)
        {
            ConditionID = conditionID;
        }

        public abstract bool ConditionCheck(T subject);
    }
}