using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StarryJamFSM;
using UnityEngine;

public partial class Cube : MonoBehaviour
{
    private FiniteStateMachine<Cube> _stateMachine;

    //获取材质
    private Material material => GetComponent<MeshRenderer>().material;
    //鼠标是否悬浮
    private bool mouseOver = false;
    //鼠标是否按下
    private bool mouseDown = false;

    private void OnMouseEnter()
    {
        mouseOver = true;
        Debug.Log("MouseOver");
    }

    private void OnMouseExit()
    {
        mouseOver = false;
        Debug.Log("MouseExit");
    }

    private void OnMouseDown()
    {
        mouseDown = true;
        Debug.Log("MouseDown");
    }

    private void OnMouseUp()
    {
        mouseDown = false;
        Debug.Log("MouseUp");
    }

    public void StateUpdate()
    {
        if (!mouseOver && !mouseDown)
        {
            material.color = Color.white;
        }
        else if (mouseOver && !mouseDown)
        {
            material.color = Color.yellow;
        }
        else if(mouseOver && mouseDown)
        {
             material.color = Color.red;
        }
    }

    private void Awake()
    {
        _stateMachine = new FiniteStateMachine<Cube>(this);

         _stateMachine
             .AddTransition(Cube_State.Default, Cube_State.MouseOver, Cube_Condition.MouseOver)
             .AddTransition(Cube_State.MouseOver, Cube_State.Default, Cube_Condition.MouseExit)
             .AddTransition(Cube_State.MouseOver, Cube_State.MouseDown, Cube_Condition.MouseDown)
             .AddTransition(Cube_State.MouseDown, Cube_State.MouseOver, Cube_Condition.MouseUp)
             .DefaultStateID = Cube_State.Default;
        
        
        _stateMachine.Awake();
    }

    private void Start()
    {
        _stateMachine.Start();
    }

    private void Update()
    {
        _stateMachine.Update();
    }
}
