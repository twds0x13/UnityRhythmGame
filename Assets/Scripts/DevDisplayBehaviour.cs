using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

public class DevDisplayBehaviour : MonoBehaviour
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Text.text = NowTime.ToString("HH:mm");
    }
}
