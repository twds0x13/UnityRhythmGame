using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Json = JsonLoader.JsonManager;

namespace ECS
{
    public class StoryTreeManager
    {
        public static readonly string RootName = "Root";

        private bool _isDeveloperMode = true;

        private Entity _rootEntity;

        private readonly ECSManager _ecsManager;

        public StoryTreeManager(ECSManager ecsManager)
        {
            _ecsManager = ecsManager ?? throw new ArgumentNullException(nameof(ecsManager));
        }

        public void SetDeveloperMode(bool enabled)
        {
            _isDeveloperMode = enabled;
        }

        // 保存故事树到文件
        public void SaveStoryTree(string filePath)
        {
            var entities = _ecsManager.GetAllEntities().ToList();
            Json.TrySaveJsonToZip(filePath, entities);
        }

        // 从文件加载故事树
        public void LoadStoryTree(string filePath)
        {
            List<Entity> entities;
            Json.TryLoadJsonFromZip(filePath, out entities);

            // 清空当前管理器
            _ecsManager.ClearAllEntities();
            _rootEntity = null;

            // 添加所有实体
            foreach (var entity in entities)
            {
                _ecsManager.AddEntity(entity);
            }

            // 重建引用关系
            _ecsManager.RebuildReferences();

            Debug.Log($"故事树已从 {filePath} 加载");
        }

        // 验证故事树结构
        public bool ValidateStoryTree()
        {
            // 检查是否有根节点
            if (!HasRoot())
            {
                Debug.Log("错误: 没有根节点");
                return false;
            }

            var root = GetOrCreateRoot();

            // 检查根节点是否有 Root 组件
            if (!root.HasComponent<Comp.Root>())
            {
                Debug.Log("错误: 根节点没有 Root 组件");
                return false;
            }

            // 检查根节点是否有父节点（不应该有）
            if (root.HasComponent<Comp.Parent>())
            {
                var parentComp = root.GetComponent<Comp.Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    Debug.Log("错误: 根节点有父节点");
                    return false;
                }
            }

            // 检查所有章节是否有正确的本地化键
            var chapters = GetChapters();
            foreach (var chapter in chapters)
            {
                if (!chapter.HasComponent<Comp.Localization>())
                {
                    Debug.Log($"错误: 章节 {chapter.Id} 没有 Localization 组件");
                    return false;
                }

                var locComp = chapter.GetComponent<Comp.Localization>();
                if (string.IsNullOrEmpty(locComp.ContextKey) || !locComp.ContextKey.StartsWith("C"))
                {
                    Debug.Log($"错误: 章节 {chapter.Id} 的本地化键不正确: {locComp.ContextKey}");
                    return false;
                }
            }

            // 可以添加更多验证逻辑...

            return true;
        }

        // 检查是否存在根节点
        public bool HasRoot()
        {
            return _rootEntity != null
                || _ecsManager
                    .FindEntities(e =>
                        !e.HasComponent<Comp.Parent>()
                        || !e.GetComponent<Comp.Parent>().ParentId.HasValue
                    )
                    .Count > 0;
        }

        public Entity GetOrCreateRoot()
        {
            if (_rootEntity != null)
                return _rootEntity;

            // 使用公共方法查找根节点
            _rootEntity = _ecsManager.FindRootEntity();

            if (_rootEntity != null)
            {
                // 确保根节点有 Root 组件
                if (!_rootEntity.HasComponent<Comp.Root>())
                {
                    _rootEntity.AddComponent(new Comp.Root { RootName = RootName });
                    Debug.LogWarning($"自动为实体 {_rootEntity.Id} 添加 Root 组件");
                }

                // 确保根节点有 ID 管理组件
                if (!_rootEntity.HasComponent<Comp.IdManager>())
                {
                    _rootEntity.AddComponent(new Comp.IdManager(0));

                    Debug.LogWarning(
                        $"自动为实体 {_rootEntity.Id} 添加 IdManager 组件，初始值设定为{0}"
                    );
                }

                if (_rootEntity.HasComponent<Comp.Parent>())
                {
                    _rootEntity.RemoveComponent<Comp.Parent>();
                    Debug.LogWarning($"自动移除 Root 实体 {_rootEntity.Id} 上的 Parent 组件");
                }

                return _rootEntity;
            }

            // 创建新的根节点 (只有在没有找到根节点时才执行)
            return CreateRoot(RootName);
        }

        /// <summary>
        /// 在无法寻找到根节点时创建根节点实体
        /// </summary>
        /// <param name="title"></param>
        /// <param name="overwrite">开启时强制抛弃现有 <see cref="_rootEntity"/> 对象</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Entity CreateRoot(string title, bool overwrite = false)
        {
            if (_rootEntity != null && !overwrite)
            {
                throw new InvalidOperationException($"根节点已存在 (ID : {_rootEntity.Id} )");
            }

            // 检查是否已存在根节点实体
            var existingRoots = _ecsManager.FindEntities(e =>
                !e.HasComponent<Comp.Parent>() || !e.GetComponent<Comp.Parent>().ParentId.HasValue
            );

            if (existingRoots.Count > 0 && !overwrite)
            {
                var existingRootIds = string.Join(", ", existingRoots.Select(r => r.Id));
                throw new InvalidOperationException($"已存在根节点实体 (IDs: {existingRootIds})");
            }

            // 如果允许覆盖，先移除现有根节点（包括异常情况下多个 Root 节点）
            if (overwrite)
            {
                if (_rootEntity != null)
                {
                    _ecsManager.RemoveEntity(_rootEntity.Id);
                    _rootEntity = null;
                }

                foreach (var existingRoot in existingRoots)
                {
                    _ecsManager.RemoveEntity(existingRoot.Id);
                }
            }

            _rootEntity = _ecsManager.CreateEntity();
            _rootEntity.AddComponent(
                new Comp.Root
                {
                    RootName = string.IsNullOrEmpty(title)
                        ? "This should not appear in Json"
                        : title,
                }
            );

            _rootEntity.AddComponent(new Comp.IdManager(0));

            // 确保根节点没有父组件
            if (_rootEntity.HasComponent<Comp.Parent>())
            {
                _rootEntity.RemoveComponent<Comp.Parent>();
            }

            return _rootEntity;
        }

        // 获取所有章节
        public List<Entity> GetChapters()
        {
            var root = GetOrCreateRoot();
            return _ecsManager
                .GetChildren(root)
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type
                        == Comp.Localization.NodeType.Chapter
                )
                .ToList();
        }

        // 获取章节的所有小节
        public List<Entity> GetEpisodes(Entity chapter)
        {
            if (chapter == null || !chapter.HasComponent<Comp.Localization>())
                return new List<Entity>();

            return _ecsManager
                .GetChildren(chapter)
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type
                        == Comp.Localization.NodeType.Episode
                )
                .ToList();
        }

        // 获取小节的所有对话行
        public List<Entity> GetLines(Entity episode)
        {
            if (episode == null || !episode.HasComponent<Comp.Localization>())
                return new List<Entity>();

            return _ecsManager
                .GetChildren(episode)
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type == Comp.Localization.NodeType.Line
                )
                .ToList();
        }

        // 创建章节节点
        public Entity CreateChapter(int number = 0, string title = "")
        {
            var chapter = _ecsManager.CreateEntity();
            var locComp = Comp.Localization.CreateChapter(number, title);
            chapter.AddComponent(locComp);

            _ecsManager.SetParent(chapter, GetOrCreateRoot());

            locComp.GenerateLocalizationKey(chapter, _ecsManager);

            return chapter;
        }

        // 创建小节节点
        public Entity CreateEpisode(Entity chapter, int number = 0, string title = "")
        {
            if (chapter == null || !chapter.HasComponent<Comp.Localization>())
                throw new ArgumentException("必须提供有效的章节实体");

            var episode = _ecsManager.CreateEntity();
            var locComp = Comp.Localization.CreateEpisode(number, title);
            episode.AddComponent(locComp);

            _ecsManager.SetParent(episode, chapter);

            // 仅在开发者模式下生成本地化键
            if (_isDeveloperMode)
            {
                locComp.GenerateLocalizationKey(episode, _ecsManager);
            }

            return episode;
        }

        // 创建对话行节点
        public Entity CreateLine(
            Entity episode,
            int number = 0,
            string dialogue = "",
            string speaker = ""
        )
        {
            if (episode == null || !episode.HasComponent<Comp.Localization>())
                throw new ArgumentException("必须提供有效的小节实体");

            var line = _ecsManager.CreateEntity();
            var locComp = Comp.Localization.CreateLine(number, dialogue, speaker);
            line.AddComponent(locComp);

            _ecsManager.SetParent(line, episode);

            // 仅在开发者模式下生成本地化键
            if (_isDeveloperMode)
            {
                locComp.GenerateLocalizationKey(line, _ecsManager);
            }

            return line;
        }

        // 获取节点的本地化键
        public string GetLocalizationKey(Entity entity)
        {
            if (entity == null || !entity.HasComponent<Comp.Localization>())
                return "";

            var locComp = entity.GetComponent<Comp.Localization>();
            return locComp.ContextKey;
        }

        // 更新节点后重新生成本地化键（仅在开发者模式下）
        public void UpdateLocalizationKey(Entity entity)
        {
            if (!_isDeveloperMode)
                return;
            if (entity == null || !entity.HasComponent<Comp.Localization>())
                return;

            var locComp = entity.GetComponent<Comp.Localization>();
            locComp.GenerateLocalizationKey(entity, _ecsManager);
        }

        // 更新所有本地化键
        public void UpdateAllLocalizationKeys(Action<Entity, Comp.Localization> updateAction = null)
        {
            foreach (var entity in _ecsManager.GetAllEntities())
            {
                if (entity.HasComponent<Comp.Localization>())
                {
                    var locComp = entity.GetComponent<Comp.Localization>();
                    updateAction?.Invoke(entity, locComp);
                }
            }
        }
    }
}
