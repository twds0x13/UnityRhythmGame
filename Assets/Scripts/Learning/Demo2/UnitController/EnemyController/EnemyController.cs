using System;
using System.Collections;
using System.Collections.Generic;
using StarryJamFSM;
using UnityEngine;

//敌人AI控制器
public partial class EnemyController : MonoBehaviour, IUnitController
{
    public UnitInstructions Instructions { get; private set; }

    private FiniteStateMachine<EnemyController> _stateMachine;
    
    //缓存玩家的GameObject
    private GameObject _player;

    public float tracingRange = 5;
    public float attackingRange = 3;

    private void Awake()
    {
        _stateMachine = new FiniteStateMachine<EnemyController>(this);
        _ConfigStateMachine();
        _stateMachine.Awake();
        //开始时获取玩家单位
        _player = GameObject.Find("Player");
    }

    //状态机配置
    void _ConfigStateMachine()
    {
        _stateMachine
            .AddTransition(EnemyController_State.Idle, EnemyController_State.Attacking,
                EnemyController_Condition.EnterAttackingRange)
            .AddTransition(EnemyController_State.Idle, EnemyController_State.Tracing,
                EnemyController_Condition.EnterTracingRange)
            .AddTransition(EnemyController_State.Tracing, EnemyController_State.Attacking,
                EnemyController_Condition.EnterAttackingRange)
            .AddTransition(EnemyController_State.Tracing, EnemyController_State.Idle,
                EnemyController_Condition.LeaveTracingRange)
            .AddTransition(EnemyController_State.Attacking, EnemyController_State.Idle,
                EnemyController_Condition.LeaveAttackingRange)
            .AddTransition(EnemyController_State.Attacking, EnemyController_State.Tracing,
                EnemyController_Condition.BetweenAttackingAndTracingRange)
            .DefaultStateID = EnemyController_State.Idle;
    }

    private void Start()
    {
        _stateMachine.Start();
    }

    private void Update()
    {
        _stateMachine.Update();
    }

    //获取和玩家的距离
    private float _GetDistanceWithPlayer()
    {
        return Vector3.Distance(transform.position, _player.transform.position);
    }
}
