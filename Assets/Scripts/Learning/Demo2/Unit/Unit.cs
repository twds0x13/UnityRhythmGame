using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StarryJamFSM;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单位类
/// </summary>
public partial class Unit : MonoBehaviour
{
    /// <summary>
    /// 最大血量
    /// </summary>
    public static readonly int maxLife = 10;
    
    /// <summary>
    /// 单位移动速度
    /// </summary>
    public float moveSpeed = 3;

    //当前血量
    public int curLife;
    
    //子弹预制体
    public Bullet bulletPrefab;

    //状态文本显示组件
    [SerializeField]
    private Text _stateDisplay;

    private string _stateTxt = "";

    //状态机
    private FiniteStateMachine<Unit> _stateMachine;

    /// <summary>
    /// 单位控制器
    /// </summary>
    public IUnitController Controller { get; set; } = new UnitDefaultController();

    private UnitInstructions _curInstructions = null;

    private void Awake()
    {
        var controller = GetComponent<IUnitController>();
        if (controller != null)
            Controller = controller;
        
        _stateMachine = new FiniteStateMachine<Unit>(this);
        curLife = maxLife;
        _ConfigStateMachine();
        _stateMachine.Awake();
    }

    private void Start()
    {
        _stateMachine.Start();
    }
    

    //状态机配置
    private void _ConfigStateMachine()
    {
        _stateMachine
            .AddTransition(Unit_State.Alive, Unit_State.Dead, Unit_Condition.ZeroLife)
            .DefaultStateID = Unit_State.Alive;
        
        _stateMachine.Open(Unit_State.Alive)
            .AddTransition(Unit_State.Executing, Unit_State.Idle, Unit_Condition.NoInstruction)
            .AddTransition(Unit_State.Idle, Unit_State.Executing, Unit_Condition.GetInstruction)
            .DefaultStateID = Unit_State.Idle;
    }

    private void Update()
    {
        _stateMachine.Update();
        _stateDisplay.text = $"Life:{curLife}\nState:{_stateTxt}";
    }
}
