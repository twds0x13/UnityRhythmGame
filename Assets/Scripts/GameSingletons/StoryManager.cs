using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ECS;
using Singleton;
using Story = ECS.StoryTreeManager;

namespace StoryNS
{
    public class StoryManager : Singleton<StoryManager>
    {
        // 故事加载状态
        public bool StoryLoaded { get; private set; } = false;

        // 加载进度（可选）
        public float LoadingProgress { get; private set; } = 0f;

        // 取消令牌源，用于取消加载
        private CancellationTokenSource _loadingCancellation;

        public void GetNextLine()
        {
            // Debug.Log("哈！");
        }

        protected override void SingletonAwake()
        {
            UniTask.Void(Test);
        }

        public async UniTaskVoid Test()
        {
            await UniTask.WaitForSeconds(0f);

            LogManager.Info("开始 ECS 测试\n", nameof(StoryManager));

            bool success = await Story.Inst.LoadStoryTreeAsync("story.zip");

            if (success)
            {
                var root = Story.Inst.GetOrCreateRoot();

                // 可以用代码添加节点，也可以用编辑器添加节点

                // 用代码删除节点可能会导致未知错误

                // 代码增删节点示例

                /*

                var chapter1 = Story.Inst.CreateChapter("Chapter 1");

                var episode1 = Story.Inst.CreateEpisode(chapter1, "Episode 1");

                var chapter2 = Story.Inst.CreateChapter("Chapter 2");

                var episode2 = Story.Inst.CreateEpisode(chapter2, "Episode 2");

                var line1 = Story.Inst.CreateLine(episode2, "E2 - Line 1");

                Story.Inst.SwapEntities(episode1, episode2);

                Story.Inst.DeleteEntity(chapter1);

                */
            }

            await Story.Inst.SaveStoryTreeAsync("story.zip");

            LogManager.Info("ECS 测试完成", nameof(StoryManager));
        }
    }
}
