using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

public class DevDisplayBehaviour : MonoBehaviour
{
    public bool IsDev;

    private TextMeshPro Text;

    private void Start()
    {
        Text = GetComponent<TextMeshPro>();
    }

    void Update()
    {
        switch (this.name)
        {
            case "DevGameTimeDisplay":
                Text.text = "Gametime :" + (Game.Inst.GetGameTime()).ToString("F2") + " S";
                break;
            case "DevAbsTimeDisplay":
                Text.text = "Abstime :" + (Game.Inst.GetAbsTime()).ToString("F2") + " S";
                break;
            case "DevTimeScaleDisplay":
                Text.text = "TimeScale x" + (Game.Inst.GetTimeScale()).ToString("F2");
                break;
            case "DevTimeScaleCacheDisplay":
                Text.text = "TimeScaleCache x" + (Game.Inst.GetTimeScaleCache()).ToString("F2");
                break;
        }
    }
}
