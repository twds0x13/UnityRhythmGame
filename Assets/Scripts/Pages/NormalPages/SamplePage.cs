using PageNS;

public class SamplePage : BaseUIPage
{
    public override void OnAwake() // ˳��һ�������������ٴ������
    {
        SetName(nameof(SamplePage));

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
