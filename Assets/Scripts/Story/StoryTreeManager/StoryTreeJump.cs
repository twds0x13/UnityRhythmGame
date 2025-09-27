using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// 获取默认开始位置（第一章第一小节第一行）
        /// </summary>
        /// <returns>指向默认开始位置的指针，如果找不到则返回null</returns>
        public StoryPointer GetDefaultStartPointer()
        {
            try
            {
                var root = GetOrCreateRoot();
                if (root == null || !root.HasComponent<Comp.Children>())
                {
                    LogManager.Warning("根节点没有子节点，无法找到默认开始位置");
                    return CreatePointer(root); // 至少返回根节点指针
                }

                // 获取第一个章节（按Order排序）
                var firstChapter = GetFirstChildByOrder(root);
                if (firstChapter == null)
                {
                    LogManager.Warning("没有找到任何章节");
                    return CreatePointer(root);
                }

                // 获取第一个小节
                var firstEpisode = GetFirstChildByOrder(firstChapter);
                if (firstEpisode == null)
                {
                    LogManager.Warning($"章节 {GetEntityDisplayName(firstChapter)} 没有小节");
                    return CreatePointer(firstChapter);
                }

                // 获取第一行
                var firstLine = GetFirstChildByOrder(firstEpisode);
                if (firstLine == null)
                {
                    LogManager.Warning($"小节 {GetEntityDisplayName(firstEpisode)} 没有对话行");
                    return CreatePointer(firstEpisode);
                }

                var pointer = CreatePointer(firstLine);
                LogManager.Log($"默认开始位置: {pointer.GetFormattedPath()}");
                return pointer;
            }
            catch (Exception ex)
            {
                LogManager.Error($"获取默认开始位置时出错: {ex.Message}");
                return CreatePointer(); // 返回根节点指针作为fallback
            }
        }

        /// <summary>
        /// 按Order顺序获取第一个子节点
        /// </summary>
        private Entity GetFirstChildByOrder(Entity parent)
        {
            if (parent?.HasComponent<Comp.Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Comp.Children>();
            if (childrenComp.ChildrenEntities.Count == 0)
                return null;

            // 按Order排序并返回第一个
            return childrenComp
                .ChildrenEntities.Where(child => child.HasComponent<Comp.Order>())
                .OrderBy(child => child.GetComponent<Comp.Order>().Number)
                .FirstOrDefault();
        }

        /// <summary>
        /// 获取实体的显示名称（用于日志）
        /// </summary>
        private string GetEntityDisplayName(Entity entity)
        {
            if (entity == null)
                return "null";

            var order = entity.GetComponent<Comp.Order>();
            var localization = entity.GetComponent<Comp.Localization>();

            if (order != null && !string.IsNullOrEmpty(order.FormattedLabel))
                return order.FormattedLabel;

            if (localization != null && !string.IsNullOrEmpty(localization.DefaultText))
                return localization.DefaultText.Length > 20
                    ? localization.DefaultText.Substring(0, 20) + "..."
                    : localization.DefaultText;

            return $"实体 {entity.Id}";
        }

        /// <summary>
        /// 快速开始剧情 - 创建指针并移动到默认开始位置
        /// </summary>
        public StoryPointer StartStory()
        {
            var pointer = GetDefaultStartPointer();
            if (pointer != null && pointer.IsValid)
            {
                LogManager.Log($"剧情开始: {pointer.GetFormattedPath()}");
            }
            return pointer;
        }

        /// <summary>
        /// 检查是否存在有效的开始位置
        /// </summary>
        public bool HasValidStartPosition()
        {
            try
            {
                var root = GetOrCreateRoot();
                if (root?.HasComponent<Comp.Children>() != true)
                    return false;

                var firstChapter = GetFirstChildByOrder(root);
                if (firstChapter?.HasComponent<Comp.Children>() != true)
                    return false;

                var firstEpisode = GetFirstChildByOrder(firstChapter);
                if (firstEpisode?.HasComponent<Comp.Children>() != true)
                    return false;

                var firstLine = GetFirstChildByOrder(firstEpisode);
                return firstLine != null;
            }
            catch
            {
                return false;
            }
        }

        public StoryPointer CreatePointer(Entity startEntity = null)
        {
            if (_rootEntity == null)
            {
                _rootEntity = GetOrCreateRoot();
            }

            return new StoryPointer(_ecsFramework, startEntity ?? _rootEntity);
        }

        /// <summary>
        /// 自动为所有episode的最后一个line配置跳转组件
        /// </summary>
        public void AutoConfigureEpisodeJumps()
        {
            var allEpisodes = _ecsFramework
                .GetAllEntities()
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type
                        == Comp.Localization.NodeType.Episode
                )
                .ToList();

            foreach (var episode in allEpisodes)
            {
                if (episode.HasComponent<Comp.Children>())
                {
                    var childrenComp = episode.GetComponent<Comp.Children>();
                    if (childrenComp.ChildrenEntities.Count > 0)
                    {
                        // 获取最后一个line（按Order排序）
                        var sortedLines = childrenComp
                            .ChildrenEntities.Where(child => child.HasComponent<Comp.Order>())
                            .OrderBy(child => child.GetComponent<Comp.Order>().Number)
                            .ToList();

                        if (sortedLines.Count > 0)
                        {
                            var lastLine = sortedLines[sortedLines.Count - 1];
                            var pointer = CreatePointer(lastLine);
                            pointer.ConfigureEpisodeJump();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 验证跳转配置
        /// </summary>
        public List<string> ValidateJumpConfigurations()
        {
            var issues = new List<string>();

            var allEntities = _ecsFramework.GetAllEntities();
            foreach (var entity in allEntities)
            {
                if (entity.HasComponent<Comp.Jump>())
                {
                    var jumpComp = entity.GetComponent<Comp.Jump>();
                    foreach (var targetId in jumpComp.AllowedTargetIds)
                    {
                        var target = _ecsFramework.GetEntitySafe(targetId);
                        if (target == null)
                        {
                            issues.Add($"实体 {entity.Id} 的跳转目标 {targetId} 不存在");
                        }
                    }
                }
            }

            return issues;
        }
    }
}
