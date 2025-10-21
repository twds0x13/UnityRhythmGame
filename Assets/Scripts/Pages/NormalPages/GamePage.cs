using PageNS;
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
    }

    public override void OnClosePage()
    {
        base.OnClosePage();

        Game.Inst.FinishGame();
    }

    public void OnExitToMenu()
    {
        OnClosePage();
    }
}
