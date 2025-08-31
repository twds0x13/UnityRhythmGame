using Cysharp.Threading.Tasks;
using ECS;
using Singleton;
using UnityEngine;
using Story = ECS.StoryTreeManager;

namespace StoryNS
{
    public class StoryManager : Singleton<StoryManager>
    {
        private ECSFramework _ecsManager;

        public ECSFramework ECSManager
        {
            get { return _ecsManager; }
        }

        public void GetNextLine()
        {
            // Debug.Log("哈！");
        }

        protected override void SingletonAwake()
        {
            _ecsManager = ECSFramework.Inst;
            UniTask.Void(Test);
        }

        public async UniTaskVoid Test()
        {
            await UniTask.WaitForSeconds(2f);

            Debug.Log("开始 ECS 测试");

            // 读取已有数据
            // Story.Inst.LoadStoryTree("story_with_localization.zip");

            // 获取或创建根节点
            var root = Story.Inst.GetOrCreateRoot();

            // Debug.Log($"根节点 ID: {root.Id}, 名称: {root.GetComponent<Comp.Root>().RootName}");

            // 启用开发者模式
            Story.Inst.SetDeveloperMode(true);

            // 创建故事结构
            var chapter1 = Story.Inst.CreateChapter(1, "第一章 开始");
            var chapter2 = Story.Inst.CreateChapter(2, "第二章 发展");
            var chapter3 = Story.Inst.CreateChapter(3, "第三章 结局");

            var episode1 = Story.Inst.CreateEpisode(chapter1, 1, "第一节 相遇");
            var episode2 = Story.Inst.CreateEpisode(chapter1, 2, "第二节 对话");

            var line1 = Story.Inst.CreateLine(episode1, 1, "你好，世界！", "主角");
            var line2 = Story.Inst.CreateLine(episode1, 2, "你好，旅行者！", "NPC");
            var line3 = Story.Inst.CreateLine(episode1, 3, "这里是什么地方？", "主角");
            var line4 = Story.Inst.CreateLine(episode2, 1, "何意味", "神笔人");

            LogFile.Log("验证树结构：");
            ECSFramework.Inst.ValidateStoryTree();
            Story.Inst.ValidateStoryTree();
            LogFile.Log("结束验证树结构：");

            for (int i = 4; i <= 10; i++)
            {
                var Chapter = Story.Inst.CreateChapter(i, $"第 {i} 章");

                for (int j = 1; j <= 10; j++)
                {
                    var Episode = Story.Inst.CreateEpisode(Chapter, j, $"第 {j} 节");
                    for (int k = 1; k <= 50; k++)
                    {
                        var Line = Story.Inst.CreateLine(Episode, k, $"第 {k} 行", "旁白");
                    }
                }
                // 保存故事树到文件
                // Story.Inst.SaveStoryTree("story_with_localization.zip");
                LogFile.Log("故事树已保存");
            }
            Story.Inst.LoadStoryTree("story_with_localization.zip");

            LogFile.Log("验证树结构：");
            ECSFramework.Inst.ValidateStoryTree();
            Story.Inst.ValidateStoryTree();
            LogFile.Log("结束验证树结构：");

            Debug.Log("ECS 测试完成");
        }
    }
}
