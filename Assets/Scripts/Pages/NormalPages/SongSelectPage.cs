using PageNS;

public class SongSelectPage : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(SongSelectPage));

        PageOpenAnimeDuration = 0.5f;

        PageCloseAnimeDuration = 0.5f;

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

    public void OnStartGame()
    {
        Manager.SwitchToPage(nameof(GamePage));
    }

    public void OnReturnToMain()
    {
        Manager.SwitchToPage(nameof(MainPage));
    }
}
