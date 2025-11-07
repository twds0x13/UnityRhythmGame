using PageNS;

public class EmptyPage : BaseUIPage
{
    public override void OnAwake() // 顺序一定是先设名字再处理基类
    {
        SetName(nameof(EmptyPage));

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
}
