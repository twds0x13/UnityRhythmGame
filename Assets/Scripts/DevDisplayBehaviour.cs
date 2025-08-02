using TMPro;
using UnityEngine;
using UserInterfaceNS;
using Game = GameManagerNS.GameManager;
using Pool = PooledObjectNS.PooledObjectManager;

public class DevDisplayBehaviour : MonoBehaviour, IPageControlled<DevDisplayBehaviour>
{
    [SerializeField]
    bool IsDev;

    [SerializeField]
    TextMeshPro Text;

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
                Text.text = "NoteUIDIterator x" + (Pool.Inst.NoteUIDIterator).ToString();
                break;
            case "DevTrackUIDIterator":
                Text.text = "TrackUIDIterator x" + (Pool.Inst.TrackUIDIterator).ToString();
                break;
            case "DevGameScoreDisplay":
                Text.text = "Score x" + (Game.Inst.Score.Score).ToString("F2");
                break;
            case "DevGameMaxScoreDisplay":
                Text.text = "MaxScore x" + (Game.Inst.Score.MaxScore).ToString();
                break;
            case "DevGameAccuracyDisplay":
                Text.text = "Accuracy x" + (Game.Inst.Score.Accuracy * 100f).ToString("F2") + "%";
                break;
        }
    }

    public void OnOpenPage()
    {
        gameObject.SetActive(true);
    }

    public void OnClosePage()
    {
        gameObject.SetActive(false);
    }
}
