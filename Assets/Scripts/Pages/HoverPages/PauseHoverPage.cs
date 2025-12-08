using PageNS;
using Game = GameManagerNS.GameManager;

public class PauseHoverPage : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(PauseHoverPage));

        PageOpenAnimeDuration = 0.15f;

        PageCloseAnimeDuration = 0.05f;

        base.OnAwake();
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();
    }

    public override void OnClosePage()
    {
        base.OnClosePage();
    }

    public void OnExitToSelect()
    {
        Game.Inst.PauseResumeGame();

        Manager.SwitchToPage<SongSelectPage>();
    }

    public void OnReturnToGame()
    {
        Game.Inst.PauseResumeGame();
    }

    public void OnRestart()
    {
        Game.Inst.PauseResumeGame();

        Manager.SwitchToPage<StartGamePage>();
    }
}
