using Cysharp.Threading.Tasks;
using PageNS;
using UnityEditor;
using UnityEngine.Localization.Settings;

public class MainPage : BaseUIPage
{
    public override void OnAwake()
    {
        SetName(nameof(MainPage));

        PageOpenAnimeDuration = 0.7f;

        PageCloseAnimeDuration = 0.4f;

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

    public void ToSongSelection()
    {
        Manager.SwitchToPage(nameof(SongSelectPage));
    }

    public void ToStory()
    {
        Manager.SwitchToPage(nameof(StoryPage));
    }

    public void SwitchLanguage()
    {
        if (LocalizationSettings.SelectedLocale.Identifier.Code == "zh")
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(
                "en"
            );
        }
        else
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(
                "zh"
            );
        }
    }

    public void GameExit()
    {
        UniTask.Void(OnGameExit);
    }

    private async UniTaskVoid OnGameExit()
    {
        await UniTask.Delay(300);

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        UnityEngine.Application.Quit();
#endif
    }
}
