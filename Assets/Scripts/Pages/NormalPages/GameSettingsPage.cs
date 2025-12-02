using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PageNS;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Chart = ChartManager;
using Pool = PooledObjectNS.PooledObjectManager;

public class GameSettingsPage : BaseUIPage
{
    [SerializeField]
    private Button switchLanguageButton;

    private CancellationTokenSource _languageSwitchCts;
    private CancellationTokenSource _pageCts;

    // 添加一个标志来跟踪语言切换状态
    private bool _isSwitchingLanguage = false;

    public override void OnAwake() // 顺序一定是先设名字再处理基类
    {
        SetName(nameof(GameSettingsPage));

        PageOpenAnimeDuration = 0.7f;
        PageCloseAnimeDuration = 0.5f;

        _pageCts = new CancellationTokenSource();

        base.OnAwake();
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();

        Pool.Inst.StartPreviewSettings();

        Chart.Inst.StartPreviewSettings();

        _pageCts?.Cancel();
        _pageCts = new CancellationTokenSource();

        // 重置语言切换状态
        _isSwitchingLanguage = false;
        if (switchLanguageButton != null)
            switchLanguageButton.interactable = true;
    }

    public override void OnClosePage()
    {
        _pageCts?.Cancel();
        _languageSwitchCts?.Cancel();
        _isSwitchingLanguage = false;

        base.OnClosePage();
    }

    public override void OnDestroyPage()
    {
        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _languageSwitchCts?.Cancel();
        _languageSwitchCts?.Dispose();

        base.OnDestroyPage();
    }

    public void SwitchLanguage()
    {
        // 防止重复点击
        if (_isSwitchingLanguage)
            return;

        if (switchLanguageButton != null)
            switchLanguageButton.interactable = false;

        _isSwitchingLanguage = true;
        _languageSwitchCts?.Cancel();
        _languageSwitchCts = new CancellationTokenSource();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _pageCts.Token,
            _languageSwitchCts.Token
        );

        UniTask.Void(() => PerformLanguageSwitch(linkedCts.Token));
    }

    private async UniTaskVoid PerformLanguageSwitch(CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // 等待本地化系统就绪
            await WaitForLocalizationReady(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            // 获取目标语言
            var currentLocale = LocalizationSettings.SelectedLocale;
            var targetLocaleCode = (currentLocale.Identifier.Code == "zh") ? "en" : "zh";
            var targetLocale = LocalizationSettings.AvailableLocales.GetLocale(targetLocaleCode);

            if (targetLocale == null)
            {
                Debug.LogError($"Target locale not found: {targetLocaleCode}");
                return;
            }

            // 执行语言切换
            LocalizationSettings.SelectedLocale = targetLocale;

            // 等待本地化系统完成语言切换
            await WaitForLocaleChangeCompletion(targetLocale, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Language switch was cancelled.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Language switch failed: {e.Message}");
        }
        finally
        {
            _isSwitchingLanguage = false;
            await SafeSetButtonInteractable(true, cancellationToken);
        }
    }

    private async UniTask WaitForLocalizationReady(CancellationToken cancellationToken)
    {
        // 如果初始化操作还在进行中，等待它完成
        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            try
            {
                await LocalizationSettings
                    .InitializationOperation.ToUniTask(cancellationToken: cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("Localization initialization timeout");
            }
            catch (OperationCanceledException)
            {
                throw; // 重新抛出取消异常
            }
        }
    }

    private async UniTask WaitForLocaleChangeCompletion(
        UnityEngine.Localization.Locale targetLocale,
        CancellationToken cancellationToken
    )
    {
        // 等待直到选中的语言确实变为目标语言
        var maxWaitTime = TimeSpan.FromSeconds(3);
        var startTime = DateTime.Now;

        while (LocalizationSettings.SelectedLocale != targetLocale)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (DateTime.Now - startTime > maxWaitTime)
            {
                Debug.LogWarning("Locale change verification timeout");
                break;
            }

            await UniTask.Yield(cancellationToken);
        }

        // 额外等待一帧确保本地化组件已经更新
        if (!cancellationToken.IsCancellationRequested)
        {
            await UniTask.Yield(cancellationToken);
        }
    }

    private async UniTask SafeSetButtonInteractable(
        bool interactable,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await UniTask.SwitchToMainThread(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            // 更严格地检查按钮有效性
            if (
                switchLanguageButton != null
                && switchLanguageButton.gameObject != null
                && switchLanguageButton.transform != null
                && // 额外的安全检查
                switchLanguageButton.gameObject.activeInHierarchy
            )
            {
                switchLanguageButton.interactable = interactable;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to set button interactable: {e.Message}");
        }
    }
}
