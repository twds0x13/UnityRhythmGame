using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using StarryJamFSM;
using UnityEngine;

namespace StarryJamFSM.Factory
{
    //条件工厂
    public class ConditionFactory
    {
        public static Condition<T> GetCondition<T>(Enum ConditionID)
        {
            Type conditionType = Enum2TypeFactory.GetType(ConditionID);
            
            ConstructorInfo ctor = conditionType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];

            return (Condition<T>)ctor.Invoke(new object[] { ConditionID });
        }
    }
}
