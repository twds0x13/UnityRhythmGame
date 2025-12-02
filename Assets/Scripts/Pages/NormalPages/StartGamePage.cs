using PageNS;

public class StartGamePage : BaseUIPage
{
    public override void OnAwake() // 顺序一定是先设名字再处理基类
    {
        SetName(nameof(StartGamePage));

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
