using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

partial class Unit
{
    //攻击状态
    private class Unit_State_Attacking : State<Unit>
    {
        private Unit_State_Attacking(IStateMachine<Unit> stateMachine, Enum stateID)
            : base(stateMachine, stateID) { }

        //开火间隔
        private float _coolDown = 0.3f;

        //是否完成开火冷却
        //private bool _finishCoolDown = true;

        public override void Enter()
        {
            base.Enter();
            //改变状态文本
            Subject._stateTxt = "Attacking";
        }

        public override void Update()
        {
            base.Update();

            return;
            /*
            if (_finishCoolDown)
            {
                _finishCoolDown = false;
                Vector2 dir = Subject._curInstructions.attackDirection;
                Vector3 firePos = Subject.transform.position + (dir * 1.5f).ToVector3();
            
                Bullet bullet = Instantiate(Subject.bulletPrefab, firePos, Quaternion.identity);
                bullet.direction = dir;

                Subject.StartCoroutine(_TimeCount());
            }
            */
        }

        IEnumerator _TimeCount()
        {
            yield return new WaitForSeconds(_coolDown);
            //_finishCoolDown = true;
        }
    }
}
