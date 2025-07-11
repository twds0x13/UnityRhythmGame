using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using StarryJamFSM;
using StarryJamFSM.Factory;
using UnityEngine;

namespace StarryJamFSM.Factory
{
    //状态工厂
    public class StateFactory
    {
        public static State<T> GetState<T>(object stateMachine, Enum stateID)
        {
            Type stateType = Enum2TypeFactory.GetType(stateID);

            ConstructorInfo ctor = stateType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];

            return (State<T>)ctor.Invoke(new object[] { stateMachine, stateID });
        }
    }
}