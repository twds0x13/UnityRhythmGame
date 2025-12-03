using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PageNS;
using UnityEditor;

public class MainPage : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(MainPage));

        PageOpenAnimeDuration = 0.7f;
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

    public override void OnDestroyPage()
    {
        base.OnDestroyPage();
    }

    public void ToSongSelection()
    {
        WaitForSongSelect().Forget();
    }

    private async UniTaskVoid WaitForSongSelect()
    {
        await UniTask.WaitForSeconds(0.0f);

        Manager.SwitchToPage(nameof(SongSelectPage));
    }

    public void ToStory()
    {
        Manager.SwitchToPage(nameof(StoryPage));
    }

    public void ToGameSettings()
    {
        Manager.SwitchToPage(nameof(GameSettingsPage));
    }

    public void GameExit()
    {
        UniTask.Void(() => OnGameExit(CancellationToken.None));
    }

    private async UniTaskVoid OnGameExit(CancellationToken cancellationToken)
    {
        try
        {
            Manager.SwitchToPage(nameof(EmptyPage));
            await UniTask.Delay(500, cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

#if UNITY_EDITOR

            EditorApplication.isPlaying = false;

#else

            Application.Quit();

#endif
        }
        catch (OperationCanceledException)
        {
            // 退出操作被取消
        }
    }
}
