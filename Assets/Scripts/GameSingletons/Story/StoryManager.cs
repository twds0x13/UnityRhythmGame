using Cysharp.Threading.Tasks;
using ECS;
using JsonLoader;
using Singleton;
using UIManagerNS;
using UnityEngine.Localization.Components;
using Story = ECS.StoryTreeManager;

/*
// 测试工具类
public class StoryTester
{
    
    public static void TestDefaultStartPosition()
    {
        LogManager.Log("=== 测试默认开始位置 ===");

        // 检查是否存在有效的开始位置
        if (!Story.Inst.HasValidStartPosition())
        {
            LogManager.Warning("没有有效的开始位置，请检查剧情结构");
            return;
        }

        // 测试获取默认开始指针
        var pointer = Story.Inst.GetDefaultStartPointer();
        if (pointer != null && pointer.IsValid)
        {
            LogManager.Log($"✓ 成功获取默认开始位置: {pointer.GetFormattedPath()}");

            // 验证节点类型
            var loc = pointer.GetLocalizationInfo();
            if (loc?.Type == Comp.Localization.NodeType.Line)
            {
                LogManager.Log("✓ 开始位置是对话行（正确）");
            }
            else
            {
                LogManager.Warning($"⚠ 开始位置是 {loc?.Type}，建议使用对话行作为开始");
            }
        }
        else
        {
            LogManager.Error("✗ 无法获取默认开始位置");
        }
    }

    public static void TestStoryFlow()
    {
        LogManager.Log("=== 测试剧情流程 ===");

        var pointer = Story.Inst.StartStory();
        if (pointer == null)
            return;

        int steps = 0;
        const int maxTestSteps = int.MaxValue;

        while (pointer.IsValid && steps < maxTestSteps)
        {
            LogManager.Log($"[步骤 {steps + 1}] {pointer.GetFormattedPath()}");

            if (!pointer.Next())
                break;

            steps++;
        }

        LogManager.Log($"测试完成，共遍历 {steps + 1} 个节点");
    }

/*
        public async UniTaskVoid TestLoadStory()
        {
            await UniTask.WaitForSeconds(0f);

            // LogManager.Info("开始 ECS 测试\n", nameof(StoryManager));

            bool success = await Story.Inst.LoadStoryTreeAsync("story.zip");

            if (success)
            {
                var root = Story.Inst.GetOrCreateRoot();

                LogManager.Log("成功加载剧情", nameof(StoryManager));

                Pointer = Story.Inst.StartStory();

                Pointer.OnChoiceEncountered += OnChioce;

                InitLocalizeEvent();

                // StoryTester.TestStoryFlow();
            }

            // await Story.Inst.SaveStoryTreeAsync("story.zip");

            // LogManager.Info("ECS 测试完成", nameof(StoryManager));
        }
}
    */

namespace StoryNS
{
    public class StoryManager : Singleton<StoryManager>
    {
        public StoryPointer Pointer { get; private set; }

        public LocalizeStringEvent StorySpeaker;

        public LocalizeStringEvent StoryContext;

        public int ChapterNumber() => Pointer.IsValid ? Pointer.ChapterNumber : 0;

        public int EpisodeNumber() => Pointer.IsValid ? Pointer.EpisodeNumber : 0;

        public int LineNumber() => Pointer.IsValid ? Pointer.LineNumber : 0;

        public void GetNextLine()
        {
            Pointer.Next();
        }

        private void OnChioceEncounter()
        {
            PageManager.Inst.OpenHoverPage(nameof(ChoiceHover));
        }

        private void OnChoiceSelect()
        {
            PageManager.Inst.CloseHoverPage(nameof(ChoiceHover));
        }

        private void OnPositionChange(int i, int j, int k)
        {
            StorySpeaker.SetEntry(Story.Inst.GetSpeakerKey(Pointer.Current));
            StoryContext.SetEntry(Story.Inst.GetContextKey(Pointer.Current));
        }

        protected override void SingletonAwake()
        {
            UniTask.Void(LoadStory);
        }

        public async UniTaskVoid LoadStory()
        {
            bool initialized = await BaseJsonLoader.EnsureInitializedAsync(); // story.zip 已经登记在需要初始化的列表中

            if (initialized)
            {
                bool success = await Story.Inst.Load("story.zip");

                if (success)
                {
                    var root = Story.Inst.GetOrCreateRoot();

                    Pointer = Story.Inst.StartStory();

                    Pointer.ChoiceEncounter(OnChioceEncounter);

                    Pointer.ChoiceSelect(OnChoiceSelect);

                    Pointer.PositionChange(OnPositionChange);

                    LogManager.Log("======Story.成功加载剧情======", nameof(StoryManager));

                    InitLocalizeEvent();
                }
            }
        }

        protected void OnDisable()
        {
            Pointer.ClearAllEvents();
        }

        private void InitLocalizeEvent()
        {
            StorySpeaker.SetTable(Story.LOCALIZATION_TABLE);
            StoryContext.SetTable(Story.LOCALIZATION_TABLE);

            OnPositionChange(0, 0, 0); // 这里的数值只是为了符合 Action 格式
        }
    }
}
