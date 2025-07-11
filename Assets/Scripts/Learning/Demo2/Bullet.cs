using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    //飞行速度
    public float speed = 10;

    //生命周期时长
    public float lifeCycle = 1;

    //飞行方向
    public Vector2 direction;

    private void Start()
    {
        //生命周期结束后自我销毁
        Destroy(gameObject, lifeCycle);
    }

    private void Update()
    {
        transform.position += direction.normalized.ToVector3() * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        //撞到单位扣血
        var unitCmp = other.GetComponent<Unit>();
        if (unitCmp)
        {
            unitCmp.curLife--;
            Destroy(gameObject);
        }
    }
}
