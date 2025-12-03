using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavigatorNS;
using PageNS;
using TMPro;
using UIManagerNS;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using CompManager = TextManagerNS.PageComponentManager;

[RequireComponent(typeof(LocalizeStringEvent)), RequireComponent(typeof(UINavigator))]
public class TextDisplay : MonoBehaviour, IPageComponent
{
    [Header("Display Controller")]
    [SerializeField]
    private List<CompManager.DynamicNum> DynamicEnumList;

    [Ext.ReadOnlyInGame, SerializeField]
    private UINavigator Navigator; // 负责控制 ParentRect ( 这个物体的 RectTransform )

    [Ext.ReadOnlyInGame, SerializeField]
    private LocalizeStringEvent LocalizeStringEvent;

    [Ext.ReadOnly, SerializeField]
    private BaseUIPage ParentPage;

    [Ext.ReadOnlyInGame, SerializeField]
    private bool TypeWriterEffect;

    [Ext.ReadOnlyInGame]
    public bool StartWithAnime;

    [Ext.ReadOnlyInGame]
    public float AnimeOffset;

    [Ext.ReadOnlyInGame, SerializeField]
    private TextMeshProUGUI SelfTextMesh;

    [Ext.ReadOnly, Range(0f, 1f), SerializeField]
    private float TypeProcess; // 暂时只用来显示 TypeProcess 进度条

    [Ext.ReadOnlyInGame, Range(1f, 30f), SerializeField]
    private float TypeSpeed;

    [Ext.ReadOnlyInGame, SerializeField]
    private bool ignoreLocalization = false; // 是否忽略本地化

    private bool ProtectFlag; // 还是得加上 CancellationToken, 这个只能临时用

    private CancellationTokenSource CancellationTokenSource; // 取消打字机 UniTask 协程

    public UnityEvent TypeWriterEvent;

    public void SetParentPage(BaseUIPage Parent) // 在 OnAwake 之后由 ParentPage 调用一次
    {
        ParentPage = Parent;
        Navigator.Init(ParentPage);
    }

    public void SetResizeDetector(ResizeDetector Detector)
    {
        Navigator.SetResizeDetector(Detector);
    }

    public void OnAwake()
    {
        gameObject.SetActive(false);

        CancellationTokenSource = new();

        if (TypeWriterEffect && !StartWithAnime)
        {
            Navigator.ActionOnCompleteAppendAnime += OnTypeWriter;
        }
    }

    public void OnOpenPage()
    {
        gameObject.SetActive(true);

        if (StartWithAnime)
        {
            OnTypeWriter();
        }

        Navigator.Append();
    }

    public void OnTypeWriter()
    {
        if (!ProtectFlag)
        {
            ProtectFlag = true;

            SelfTextMesh.alpha = 0f;

            if (!ignoreLocalization)
            {
                SelfTextMesh.text = null;
            }

            Offset(CancellationTokenSource.Token).Forget();
        }
    }

    private async UniTaskVoid Offset(CancellationToken Token) // 看起来能用就不要改
    {
        try
        {
            Token.ThrowIfCancellationRequested();

            await UniTask.WaitForSeconds(AnimeOffset);

            TypeWriterCoroutine(CancellationTokenSource.Token).Forget();
        }
        catch (OperationCanceledException) { }
    }

    private async UniTaskVoid TypeWriterCoroutine(CancellationToken Token)
    {
        TypeProcess = 0f;

        if (!ignoreLocalization)
        {
            SelfTextMesh.text = LocalizeStringEvent.StringReference.GetLocalizedString();
        }

        SelfTextMesh.alpha = 1f;

        try
        {
            for (int i = 1; i < SelfTextMesh.text.Length + 1; i++)
            {
                Token.ThrowIfCancellationRequested();

                TypeProcess = (float)i / SelfTextMesh.text.Length;

                SelfTextMesh.maxVisibleCharacters = i;

                await UniTask.WaitForSeconds(
                    LocalizationSettings.SelectedLocale.Identifier.Code == "en"
                        ? 0.2f / 3f / TypeSpeed
                        : 0.2f / TypeSpeed
                );
            }

            ProtectFlag = false;
        }
        catch (OperationCanceledException) { }
    }

    public void OnNextLine()
    {
        if (ProtectFlag)
        {
            OnSkipTypeWriter();
        }
        else
        {
            TypeProcess = 0f; // 不会因为设置Unitask闪一下

            OnTypeWriter();

            TypeWriterEvent?.Invoke();
        }
    }

    private void OnSkipTypeWriter()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = new();
        TypeProcess = 1f;
        SelfTextMesh.maxVisibleCharacters = SelfTextMesh.text.Length;
        ProtectFlag = false;
    }

    public void RefreshText()
    {
        if (ignoreLocalization)
            return;

        LocalizeStringEvent.RefreshString();
    }

    public void OnClosePage()
    {
        Navigator.Disappear();
    }

    private void Update()
    {
        if (DynamicEnumList.Count > 0)
        {
            LocalizeStringEvent.StringReference.Arguments = CompManager.Inst.GetDynamicNumsFromList(
                DynamicEnumList
            );

            RefreshText();
        }
    }
}
