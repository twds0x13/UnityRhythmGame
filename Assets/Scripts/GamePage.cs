using UnityEngine;
using Game = GameManagerNS.GameManager;
using Page = UserInterfaceNS.UserInterfaceManager;
using Pool = PooledObjectNS.PooledObjectManager;

public class GamePage : BaseUIPage
{
    public override void OnUpdatePage()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Page.Inst.SwitchToPage(Page.Inst.AnotherPage);
        }
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();
        Pool.Inst.GetTracksDynamic();
    }

    public override void OnClosePage()
    {
        base.OnClosePage();
        Pool.Inst.ResetGame();
        Game.Inst.ResetGame();
    }
}
