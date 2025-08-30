using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using ECS;
using Singleton;
using UnityEngine;

namespace StoryNS
{
    public class StoryManager : Singleton<StoryManager>
    {
        private ECSManager _storyManager;

        public ECSManager ECSManager
        {
            get
            {
                if (_storyManager == null)
                {
                    _storyManager = new ECSManager();
                }
                return _storyManager;
            }
        }

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
            await UniTask.WaitForSeconds(1);
            Debug.Log("开始 ECS 测试");

            // 创建 ECSManager 和 StoryTreeManager 实例
            var ecsManager = new ECSManager();
            var storyManager = new StoryTreeManager(ecsManager);

            // 获取或创建根节点
            var root = storyManager.GetOrCreateRoot();
            Debug.Log($"根节点 ID: {root.Id}, 名称: {root.GetComponent<Comp.Root>().RootName}");

            // 启用开发者模式
            storyManager.SetDeveloperMode(true);

            // 创建故事结构
            var chapter1 = storyManager.CreateChapter(1, "第一章 开始");
            var chapter2 = storyManager.CreateChapter(2, "第二章 发展");

            var episode1 = storyManager.CreateEpisode(chapter1, 1, "第一节 相遇");
            var episode2 = storyManager.CreateEpisode(chapter1, 2, "第二节 对话");

            var line1 = storyManager.CreateLine(episode1, 1, "你好，世界！", "主角");
            var line2 = storyManager.CreateLine(episode1, 2, "你好，旅行者！", "NPC");
            var line3 = storyManager.CreateLine(episode1, 3, "这里是什么地方？", "主角");

            // 获取本地化键（在开发者模式下已生成）
            Debug.Log($"Chapter 1 Key: {storyManager.GetLocalizationKey(chapter1)}"); // 输出: C1
            Debug.Log($"Episode 1 Key: {storyManager.GetLocalizationKey(episode1)}"); // 输出: C1_E1
            Debug.Log($"Line 1 Key: {storyManager.GetLocalizationKey(line1)}"); // 输出: C1_E1_L1

            // 验证根节点没有 Localization 组件
            Debug.Log($"根节点有 Localization 组件: {root.HasComponent<Comp.Localization>()}"); // 输出: False
            Debug.Log($"根节点有 Root 组件: {root.HasComponent<Comp.Root>()}"); // 输出: True

            // 验证章节有正确的父节点（应该是根节点）
            if (chapter1.HasComponent<Comp.Parent>())
            {
                var parentComp = chapter1.GetComponent<Comp.Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    var parentEntity = ecsManager.GetEntity(parentComp.ParentId.Value);
                    Debug.Log($"Chapter 1 的父节点 ID: {parentComp.ParentId.Value}");
                    Debug.Log($"Chapter 1 的父节点是根节点: {parentEntity == root}");
                }
            }

            // 保存故事树到文件
            storyManager.SaveStoryTree("story_with_localization.zip");
            Debug.Log("故事树已保存");

            // 禁用开发者模式
            storyManager.SetDeveloperMode(false);

            // 在非开发者模式下，创建节点不会自动生成本地化键
            var chapter3 = storyManager.CreateChapter(3, "第三章 结局");
            Debug.Log($"Chapter 3 Key: {storyManager.GetLocalizationKey(chapter3)}"); // 输出: 空字符串

            // 重新启用开发者模式并更新所有键
            storyManager.SetDeveloperMode(true);
            storyManager.UpdateAllLocalizationKeys();

            // 重新获取 Chapter 3 的键
            Debug.Log($"Chapter 3 Key: {storyManager.GetLocalizationKey(chapter3)}"); // 输出: C3

            // 保存更新后的故事树
            storyManager.SaveStoryTree("story_updated.zip");
            Debug.Log("更新后的故事树已保存");

            // 加载故事树
            var newEcsManager = new ECSManager();
            var newStoryManager = new StoryTreeManager(newEcsManager);
            newStoryManager.LoadStoryTree("story_updated.zip");

            // 检查加载后的根节点
            var loadedRoot = newStoryManager.GetOrCreateRoot();
            Debug.Log(
                $"加载的根节点 ID: {loadedRoot.Id}, 名称: {loadedRoot.GetComponent<Comp.Root>().RootName}"
            );

            // 检查加载后的本地化键
            var loadedChapters = newStoryManager.GetChapters();
            if (loadedChapters.Count > 0)
            {
                var loadedChapter1 = loadedChapters[0];
                Debug.Log(
                    $"Loaded Chapter 1 Key: {newStoryManager.GetLocalizationKey(loadedChapter1)}"
                ); // 输出: C1
            }

            // 验证加载后的结构完整性
            bool isValid = newStoryManager.ValidateStoryTree();
            Debug.Log($"加载后的故事树验证: {(isValid ? "通过" : "失败")}");

            // 测试根节点管理方法
            try
            {
                // 尝试创建另一个根节点（应该会失败）
                var anotherRoot = newStoryManager.CreateRoot("另一个根节点");
                Debug.Log("错误: 应该不允许创建多个根节点");
            }
            catch (InvalidOperationException ex)
            {
                Debug.Log($"预期中的错误: {ex.Message}");
            }

            // 测试覆盖选项
            try
            {
                // 使用覆盖选项创建新根节点
                var newRoot = newStoryManager.CreateRoot("新根节点", overwrite: true);
                Debug.Log(
                    $"新根节点 ID: {newRoot.Id}, 名称: {newRoot.GetComponent<Comp.Root>().RootName}"
                );
            }
            catch (Exception ex)
            {
                Debug.Log($"创建新根节点时出错: {ex.Message}");
            }

            Debug.Log("ECS 测试完成");
        }
    }
}
