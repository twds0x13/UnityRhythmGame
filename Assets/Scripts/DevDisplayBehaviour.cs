using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

public class DevDisplayBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    public TextMeshProUGUI Text;

    void Start()
    {
        Text = transform.GetComponent<TextMeshProUGUI>();
        // TxtCurrentTime = GetComponent<TextMeshPro> ();
    }

    // Update is called once per frame
    void Update()
    {
        //获取系统当前时间

        Text.text = NowTime.ToString("HH:mm");
    }
}
