using PageNS;
using Ctrl = GameCore.GameController;
using Game = GameManagerNS.GameManager;

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
    }

    public override void OnClosePage()
    {
        base.OnClosePage();

        Game.Inst.ExitGame();

        Ctrl.Inst.ExitGame();
    }

    public void OnExitToMenu()
    {
        OnClosePage();
    }
}
