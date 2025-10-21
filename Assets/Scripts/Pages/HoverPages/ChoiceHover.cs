using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ECS;
using PageNS;
using UnityEngine.Localization.Components;
using Story = StoryNS.StoryManager;

public class ChoiceHover : BaseUIPage
{
    [Ext.ReadOnlyInGame]
    public List<LocalizeStringEvent> OptionLocalizeEvents;

    public override void OnAwake()
    {
        SetName(nameof(ChoiceHover));

        PageOpenAnimeDuration = 0.5f;

        PageCloseAnimeDuration = 0.5f;

        base.OnAwake();
    }

    public async UniTaskVoid DisplayOptions()
    {
        await UniTask.Yield(); // ���û�еȴ��Ļ������ܻ���� Story.Inst.Pointer ��û�и��µ������ �ű�ִ��˳������ ��

        for (int i = 0; i < Story.Inst.Pointer.ChoiceOptions.Count; i++)
        {
            var option = Story.Inst.Pointer.ChoiceOptions[i];
            LogManager.Log(option.LocalizationKey);
            OptionLocalizeEvents[i].SetTable(StoryTreeManager.LOCALIZATION_TABLE);
            OptionLocalizeEvents[i].SetEntry(option.LocalizationKey);
        }
    }

    public void Choice(int num)
    {
        Story.Inst.Pointer.Choice(num);
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();

        UniTask.Void(DisplayOptions);
    }

    public override void OnClosePage()
    {
        base.OnClosePage();
    }
}
