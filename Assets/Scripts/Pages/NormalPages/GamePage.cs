using DG.Tweening;
using PageNS;
using UnityEngine;
using Game = GameManagerNS.GameManager;
using Pool = PooledObjectNS.PooledObjectManager;

public class GamePage : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(GamePage));

        PageOpenAnimeDuration = 0.5f;

        PageCloseAnimeDuration = 0.5f;

        ActionFinishClosePage = () =>
        {
            Manager.JumpToPage(nameof(MainPage));
        };

        base.OnAwake();
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();

        Pool.Inst.GetTracksDynamic();
    }

    public override void OnClosePage()
    {
        base.OnClosePage();

        Pool.Inst.FinishCurrentGame();

        Game.Inst.ResetGame();
    }

    public void OnExitToMenu()
    {
        OnClosePage();
    }
}
