using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家控制器
/// </summary>
public class PlayerController : MonoBehaviour, IUnitController
{
    public UnitInstructions Instructions { get; private set; }

    private void Update()
    {
        //移动控制
        Vector2 moveDir = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDir += Vector2.up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDir += Vector2.left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDir += Vector2.down;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir += Vector2.right;
        }

        
        //攻击控制
        Vector2 attackDir = Vector2.zero;
        
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, 1000, 1 << LayerMask.NameToLayer("Floor"));
            attackDir = (hit.point - transform.position).ToVector2().normalized;
        }

        if (attackDir == Vector2.zero && moveDir == Vector2.zero)
            Instructions = null;
        else
        {
            Instructions = new UnitInstructions();
            Instructions.attackDirection = attackDir;
            Instructions.moveDirection = moveDir;
        }
        
        //在调试模式显示一下攻击移动方向
        Debug.DrawRay(transform.position, attackDir.ToVector3(), Color.green);
        Debug.DrawRay(transform.position, moveDir.ToVector3(), Color.red);
    }
}
