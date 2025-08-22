using PageNS;

public class StoryPage : BaseUIPage
{
    public override void OnAwake() // 已解除作为剧情编辑器的任务，只是剧情展示
    {
        SetName(nameof(StoryPage));

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

    public void ReturnToMain()
    {
        Manager.SwitchToPage(nameof(MainPage));
    }

    public void ReturnToGame()
    {
        Manager.SwitchToPage(nameof(GamePage));
    }
}
