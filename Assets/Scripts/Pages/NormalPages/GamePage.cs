using PageNS;
using Chart = ChartManager;
using Ctrl = GameCore.GameController;
using Game = GameManagerNS.GameManager;

public class GamePage : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(GamePage));

        PageOpenAnimeDuration = 0.5f;

        PageCloseAnimeDuration = 0.5f;

        base.OnAwake();
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();

        Game.Inst.StartGame();

        Chart.Inst.StartGame();
    }

    public override void OnClosePage()
    {
        base.OnClosePage();

        Chart.Inst.ExitGame();

        Game.Inst.ExitGame();
    }

    public void ToggleAutoPlay()
    {
        Ctrl.Inst.ToggleAutoPlay();
    }
}
