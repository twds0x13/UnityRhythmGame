using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NavigatorNS;
using PageNS;
using TMPro;
using UIManagerNS;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using Comp = TextManagerNS.PageComponentManager;
using Story = StoryNS.StoryManager;

[RequireComponent(typeof(LocalizeStringEvent)), RequireComponent(typeof(UINavigator))]
public class TextDisplay : MonoBehaviour, IPageComponent
{
    [Header("Display Controller")]
    [SerializeField]
    private List<Comp.DynamicNum> DynamicEnumList;

    [Ext.ReadOnlyInGame, SerializeField]
    private UINavigator Navigator; // ������� ParentRect ( �������� RectTransform )

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
    private float TypeProcess; // ��ʱֻ������ʾ TypeProcess ������

    [Ext.ReadOnlyInGame, Range(1f, 10f), SerializeField]
    private float TypeSpeed;

    private bool ProtectFlag; // ���ǵü��� CancellationToken, ���ֻ����ʱ��

    private CancellationTokenSource CancellationTokenSource; // ȡ�����ֻ� UniTask Э��

    public void SetParentPage(BaseUIPage Parent) // �� OnAwake ֮���� ParentPage ����һ��
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

            SelfTextMesh.text = null;

            Offset(CancellationTokenSource.Token).Forget();
        }
    }

    private async UniTaskVoid Offset(CancellationToken Token) // ��Ҫ���� CancellationToken ���������סʱ���� await ��δ���ʱ�˳� �Ῠ�� TypeWriterCoroutine ��ɿ�����
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

        SelfTextMesh.text = LocalizeStringEvent.StringReference.GetLocalizedString();

        try
        {
            for (int i = 1; i < SelfTextMesh.text.Length + 1; i++)
            {
                Token.ThrowIfCancellationRequested();

                TypeProcess = (float)i / SelfTextMesh.text.Length;

                SelfTextMesh.maxVisibleCharacters = i;

                await UniTask.WaitForSeconds(
                    LocalizationSettings.SelectedLocale.Identifier.Code == "en"
                        ? 0.2f / TypeSpeed
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
            Story.Inst.GetNextLine();
            OnTypeWriter();
        }
    }

    private void GetNextLine() { }

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
            LocalizeStringEvent.StringReference.Arguments = Comp.Inst.GetDynamicNumsFromList(
                DynamicEnumList
            );

            RefreshText();
        }
    }
}
