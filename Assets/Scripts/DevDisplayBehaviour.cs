using TMPro;
using UnityEngine;
using Game = GameManager.GameManager;
using Main = GameCore.GameController;
using Pool = PooledObject.PooledObjectManager;

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
        switch (name)
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
            case "DevNoteCountInactiveDisplay":
                Text.text = "NoteInactive x" + (Pool.Inst.GetNotePoolCountInactive()).ToString();
                break;
            case "DevNoteUIDIterator":
                Text.text = "NoteUIDIterator x" + (Pool.Inst.GetNoteUIDIterator()).ToString();
                break;
            case "DevTrackUIDIterator":
                Text.text = "TrackUIDIterator x" + (Pool.Inst.GetTrackUIDIterator()).ToString();
                break;
        }
    }
}
