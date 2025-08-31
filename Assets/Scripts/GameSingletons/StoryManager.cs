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
            await UniTask.WaitForSeconds(0f);

            Debug.Log("开始 ECS 测试");

            var root = Story.Inst.GetOrCreateRoot();

            // 读取已有数据
            Story.Inst.LoadStoryTree("story_with_localization.zip");

            // 获取或创建根节点

            /*
            // Debug.Log($"根节点 ID: {root.Id}, 名称: {root.GetComponent<Comp.Root>().RootName}");

            // 启用开发者模式
            Story.Inst.SetDeveloperMode(true);

            // 创建故事结构
            var chapter1 = Story.Inst.CreateChapter(1, "第一章 开始");
            var chapter2 = Story.Inst.CreateChapter(2, "第二章 发展");

            var episode1 = Story.Inst.CreateEpisode(chapter1, 1, "第一节 相遇");
            var episode2 = Story.Inst.CreateEpisode(chapter1, 2, "第二节 对话");

            var line1 = Story.Inst.CreateLine(episode1, 1, "你好，世界！", "主角");
            var line2 = Story.Inst.CreateLine(episode1, 2, "你好，旅行者！", "NPC");
            var line3 = Story.Inst.CreateLine(episode1, 3, "这里是什么地方？", "主角");
            var line4 = Story.Inst.CreateLine(episode2, 1, "何意味", "神笔人");

            // 获取本地化键（在开发者模式下已生成）
            Debug.Log($"Chapter 1 Key: {Story.Inst.GetLocalizationKey(chapter1)} "); // 输出: C1
            Debug.Log($"Episode 1 Key: {Story.Inst.GetLocalizationKey(episode1)} "); // 输出: C1_E1
            Debug.Log($"Line 1.4 Key: {Story.Inst.GetLocalizationKey(line4)} "); // 输出: C1_E1_L1
            */
            // 验证根节点没有 Localization 组件
            Debug.Log($"根节点有 Localization 组件: {root.HasComponent<Comp.Localization>()}"); // 输出: False
            Debug.Log($"根节点有 Root 组件: {root.HasComponent<Comp.Root>()}"); // 输出: True

            Debug.Log("验证树结构：");
            ECSFramework.Inst.ValidateStoryTree();
            Story.Inst.ValidateStoryTree();
            Debug.Log("结束验证树结构：");

            // 保存故事树到文件
            Story.Inst.SaveStoryTree("story_with_localization.zip");
            Debug.Log("故事树已保存");
            /*
            // 禁用开发者模式
            Story.Inst.SetDeveloperMode(false);

            var chapter3 = Story.Inst.CreateChapter(3, "第三章 结局");
            Debug.Log($"Chapter 3 Key: {Story.Inst.GetLocalizationKey(chapter3)}"); // 输出: C3

            // 保存更新后的故事树
            Story.Inst.SaveStoryTree("story_updated.zip");
            Debug.Log("更新后的故事树已保存");
            */
            Debug.Log("ECS 测试完成");
        }
    }
}
