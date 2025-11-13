using GameManagerNS;
using PageNS;
using Chart = ChartManager;
using Ctrl = GameCore.GameController;
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

        Game.Inst.StartGame();

        Ctrl.Inst.StartGame();

        Chart.Inst.StartGame(Game.Inst.GetGameTime());
    }

    public override void OnClosePage()
    {
        base.OnClosePage();

        Game.Inst.ExitGame();

        Chart.Inst.ExitGame();

        Ctrl.Inst.ExitGame();

        Pool.Inst.ExitGame();
    }

    public void OnExitToMenu()
    {
        OnClosePage();
    }
}
