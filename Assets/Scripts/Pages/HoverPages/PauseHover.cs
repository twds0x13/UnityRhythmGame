using PageNS;

public class PauseHover : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(PauseHover));

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
}
