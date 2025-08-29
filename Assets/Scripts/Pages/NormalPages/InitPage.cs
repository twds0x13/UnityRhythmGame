using PageNS;

public class InitPage : BaseUIPage
{
    public override void OnAwake() // 顺序一定是先设名字再处理基类
    {
        SetName(nameof(InitPage));

        PageOpenAnimeDuration = 0.0f;

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

    public void OnToMainPage()
    {
        Manager.SwitchToPage(nameof(MainPage));
    }
}
