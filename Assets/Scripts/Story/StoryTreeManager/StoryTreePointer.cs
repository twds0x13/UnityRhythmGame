using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// 剧情遍历指针，提供节点导航和组件扫描功能
    /// </summary>
    public class StoryPointer
    {
        private Entity _currentEntity;
        private readonly ECSFramework _ecsFramework;
        private readonly Stack<Entity> _history = new Stack<Entity>();

        /// <summary>
        /// 当前指向的实体
        /// </summary>
        public Entity Current => _currentEntity;

        /// <summary>
        /// 当前实体ID
        /// </summary>
        public int CurrentId => _currentEntity?.Id ?? -1;

        /// <summary>
        /// 是否有历史记录（可以返回）
        /// </summary>
        public bool HasHistory => _history.Count > 0;

        /// <summary>
        /// 是否指向有效实体
        /// </summary>
        public bool IsValid => _currentEntity != null;

        public StoryPointer(ECSFramework ecsFramework, Entity startEntity = null)
        {
            _ecsFramework = ecsFramework ?? throw new ArgumentNullException(nameof(ecsFramework));
            _currentEntity = startEntity;
        }

        /// <summary>
        /// 移动到指定实体
        /// </summary>
        public bool MoveTo(int entityId)
        {
            var target = _ecsFramework.GetEntitySafe(entityId);
            if (target != null)
            {
                _history.Push(_currentEntity);
                _currentEntity = target;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动到指定实体（带历史记录）
        /// </summary>
        public bool MoveToWithHistory(Entity entity)
        {
            if (entity != null)
            {
                _history.Push(_currentEntity);
                _currentEntity = entity;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 返回到上一个节点
        /// </summary>
        public bool Back()
        {
            if (_history.Count > 0)
            {
                _currentEntity = _history.Pop();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// 快速移动到默认开始位置（第一章第一小节第一行）
        /// </summary>
        public bool MoveToDefaultStart()
        {
            var defaultStart = StoryTreeManager.Inst.GetDefaultStartPointer();
            if (defaultStart != null && defaultStart.IsValid)
            {
                return MoveToWithHistory(defaultStart.Current);
            }
            return false;
        }

        /// <summary>
        /// 重置到默认开始位置（清空历史）
        /// </summary>
        public void ResetToDefaultStart()
        {
            var defaultStart = StoryTreeManager.Inst.GetDefaultStartPointer();
            if (defaultStart != null && defaultStart.IsValid)
            {
                ClearHistory();
                _currentEntity = defaultStart.Current;
                LogManager.Log($"已重置到默认开始位置: {GetFormattedPath()}");
            }
        }

        /// <summary>
        /// 开始新的剧情会话（重置到开始位置并清空历史）
        /// </summary>
        public void StartNewSession()
        {
            ResetToDefaultStart();
            ClearHistory();
        }

        #region 树状结构导航

        /// <summary>
        /// 移动到父节点
        /// </summary>
        public bool MoveToParent()
        {
            if (_currentEntity?.HasComponent<Comp.Parent>() == true)
            {
                var parentComp = _currentEntity.GetComponent<Comp.Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    return MoveTo(parentComp.ParentId.Value);
                }
            }
            return false;
        }

        /// <summary>
        /// 移动到第一个子节点
        /// </summary>
        public bool MoveToFirstChild()
        {
            if (_currentEntity?.HasComponent<Comp.Children>() == true)
            {
                var childrenComp = _currentEntity.GetComponent<Comp.Children>();
                if (childrenComp.ChildrenEntities.Count > 0)
                {
                    return MoveTo(childrenComp.ChildrenEntities[0].Id);
                }
            }
            return false;
        }

        /// <summary>
        /// 移动到指定索引的子节点
        /// </summary>
        public bool MoveToChildAt(int index)
        {
            if (_currentEntity?.HasComponent<Comp.Children>() == true)
            {
                var childrenComp = _currentEntity.GetComponent<Comp.Children>();
                if (index >= 0 && index < childrenComp.ChildrenEntities.Count)
                {
                    return MoveTo(childrenComp.ChildrenEntities[index].Id);
                }
            }
            return false;
        }

        /// <summary>
        /// 移动到下一个兄弟节点
        /// </summary>
        public bool MoveToNextSibling()
        {
            if (_currentEntity == null)
                return false;

            // 获取父节点
            if (!MoveToParent())
                return false;

            var parent = _currentEntity;
            if (!parent.HasComponent<Comp.Children>())
                return false;

            var childrenComp = parent.GetComponent<Comp.Children>();
            var children = childrenComp.ChildrenEntities;

            // 找到当前节点在兄弟中的位置
            int currentIndex = -1;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Id == _history.Peek()?.Id) // 上一个节点是原来的当前节点
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex >= 0 && currentIndex < children.Count - 1)
            {
                return MoveTo(children[currentIndex + 1].Id);
            }

            return false;
        }

        /// <summary>
        /// 移动到上一个兄弟节点
        /// </summary>
        public bool MoveToPreviousSibling()
        {
            if (_currentEntity == null)
                return false;

            // 获取父节点
            if (!MoveToParent())
                return false;

            var parent = _currentEntity;
            if (!parent.HasComponent<Comp.Children>())
                return false;

            var childrenComp = parent.GetComponent<Comp.Children>();
            var children = childrenComp.ChildrenEntities;

            // 找到当前节点在兄弟中的位置
            int currentIndex = -1;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Id == _history.Peek()?.Id)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex > 0)
            {
                return MoveTo(children[currentIndex - 1].Id);
            }

            return false;
        }

        #endregion

        #region 跳转功能

        /// <summary>
        /// 检查是否可以跳转到目标节点
        /// </summary>
        public bool CanJumpTo(int targetId)
        {
            if (_currentEntity?.HasComponent<Comp.Jump>() == true)
            {
                var jumpComp = _currentEntity.GetComponent<Comp.Jump>();
                return jumpComp.CanJumpTo(targetId);
            }
            return false;
        }

        /// <summary>
        /// 执行跳转到目标节点
        /// </summary>
        public bool JumpTo(int targetId)
        {
            if (CanJumpTo(targetId))
            {
                return MoveTo(targetId);
            }
            return false;
        }

        /// <summary>
        /// 获取所有可跳转的目标
        /// </summary>
        public List<Entity> GetJumpTargets()
        {
            var targets = new List<Entity>();

            if (_currentEntity?.HasComponent<Comp.Jump>() == true)
            {
                var jumpComp = _currentEntity.GetComponent<Comp.Jump>();
                foreach (var targetId in jumpComp.AllowedTargetIds)
                {
                    var target = _ecsFramework.GetEntitySafe(targetId);
                    if (target != null)
                    {
                        targets.Add(target);
                    }
                }
            }

            return targets;
        }

        #endregion

        #region 组件扫描功能

        /// <summary>
        /// 扫描当前节点的特定类型组件
        /// </summary>
        public T ScanComponent<T>()
            where T : IComponent
        {
            return _currentEntity.GetComponent<T>();
        }

        /// <summary>
        /// 扫描当前节点是否包含特定类型组件
        /// </summary>
        public bool HasComponent<T>()
            where T : IComponent
        {
            return _currentEntity?.HasComponent<T>() == true;
        }

        /// <summary>
        /// 扫描子节点中的特定类型组件
        /// </summary>
        public List<T> ScanChildrenComponents<T>()
            where T : IComponent
        {
            var results = new List<T>();

            if (_currentEntity?.HasComponent<Comp.Children>() == true)
            {
                var childrenComp = _currentEntity.GetComponent<Comp.Children>();
                foreach (var child in childrenComp.ChildrenEntities)
                {
                    var component = child.GetComponent<T>();
                    if (component != null)
                    {
                        results.Add(component);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 在子树中扫描特定类型组件（深度优先）
        /// </summary>
        public List<T> ScanSubtreeComponents<T>(int maxDepth = 3)
            where T : IComponent
        {
            var results = new List<T>();
            ScanSubtreeRecursive(_currentEntity, 0, maxDepth, results);
            return results;
        }

        private void ScanSubtreeRecursive<T>(
            Entity entity,
            int currentDepth,
            int maxDepth,
            List<T> results
        )
            where T : IComponent
        {
            if (entity == null || currentDepth > maxDepth)
                return;

            // 扫描当前节点的组件
            var component = entity.GetComponent<T>();
            if (component != null)
            {
                results.Add(component);
            }

            // 递归扫描子节点
            if (entity.HasComponent<Comp.Children>() && currentDepth < maxDepth)
            {
                var childrenComp = entity.GetComponent<Comp.Children>();
                foreach (var child in childrenComp.ChildrenEntities)
                {
                    ScanSubtreeRecursive(child, currentDepth + 1, maxDepth, results);
                }
            }
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 获取当前节点的本地化信息
        /// </summary>
        public Comp.Localization GetLocalizationInfo()
        {
            return ScanComponent<Comp.Localization>();
        }

        /// <summary>
        /// 获取当前节点的顺序信息
        /// </summary>
        public Comp.Order GetOrderInfo()
        {
            return ScanComponent<Comp.Order>();
        }

        /// <summary>
        /// 获取格式化路径（如：第1章 / 第2节 / 3.）
        /// </summary>
        public string GetFormattedPath()
        {
            var path = new List<string>();
            var tempPointer = new StoryPointer(_ecsFramework, _currentEntity);

            // 向上遍历构建路径
            while (tempPointer.IsValid)
            {
                var order = tempPointer.GetOrderInfo();
                if (order != null)
                {
                    path.Insert(0, order.FormattedLabel);
                }

                if (!tempPointer.MoveToParent())
                    break;
            }

            return string.Join(" / ", path);
        }

        /// <summary>
        /// 重置指针到根节点
        /// </summary>
        public void ResetToRoot()
        {
            var root = _ecsFramework
                .GetAllEntities()
                .FirstOrDefault(e => e.HasComponent<Comp.Root>());

            if (root != null)
            {
                ClearHistory();
                _currentEntity = root;
            }
        }

        #endregion

        #region 默认顺序跳转逻辑

        /// <summary>
        /// 获取当前节点的下一个节点（按Order顺序）
        /// </summary>
        public Entity GetNextByOrder()
        {
            if (_currentEntity == null)
                return null;

            // 如果有跳转组件且不是默认顺序类型，优先使用跳转逻辑
            if (_currentEntity.HasComponent<Comp.Jump>())
            {
                var jumpComp = _currentEntity.GetComponent<Comp.Jump>();
                if (
                    jumpComp.Type != Comp.JumpType.DefaultOrder
                    && jumpComp.AllowedTargetIds.Count > 0
                )
                {
                    var target = _ecsFramework.GetEntitySafe(jumpComp.AllowedTargetIds[0]);
                    if (target != null)
                        return target;
                }
            }

            // 尝试获取下一个兄弟节点
            var nextSibling = GetNextSiblingByOrder();
            if (nextSibling != null)
                return nextSibling;

            // 如果没有兄弟节点，尝试获取父节点的下一个兄弟节点的第一个子节点
            return GetNextEpisodeFirstLine();
        }

        /// <summary>
        /// 按Order顺序获取下一个兄弟节点
        /// </summary>
        private Entity GetNextSiblingByOrder()
        {
            if (_currentEntity?.HasComponent<Comp.Parent>() != true)
                return null;

            var parentComp = _currentEntity.GetComponent<Comp.Parent>();
            if (!parentComp.ParentId.HasValue)
                return null;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Comp.Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Comp.Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);

            int currentIndex = -1;
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                if (sortedChildren[i].Id == _currentEntity.Id)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex >= 0 && currentIndex < sortedChildren.Count - 1)
            {
                return sortedChildren[currentIndex + 1];
            }

            return null;
        }

        /// <summary>
        /// 获取下一个episode的第一个line节点
        /// </summary>
        private Entity GetNextEpisodeFirstLine()
        {
            if (_currentEntity?.HasComponent<Comp.Parent>() != true)
                return null;

            // 获取当前line的父episode
            var lineParentComp = _currentEntity.GetComponent<Comp.Parent>();
            if (!lineParentComp.ParentId.HasValue)
                return null;

            var episode = _ecsFramework.GetEntitySafe(lineParentComp.ParentId.Value);
            if (episode?.HasComponent<Comp.Parent>() != true)
                return null;

            // 获取episode的父chapter
            var episodeParentComp = episode.GetComponent<Comp.Parent>();
            if (!episodeParentComp.ParentId.HasValue)
                return null;

            var chapter = _ecsFramework.GetEntitySafe(episodeParentComp.ParentId.Value);
            if (chapter?.HasComponent<Comp.Children>() != true)
                return null;

            var chapterChildrenComp = chapter.GetComponent<Comp.Children>();
            var sortedEpisodes = GetSortedChildrenByOrder(chapterChildrenComp.ChildrenEntities);

            // 找到当前episode在chapter中的位置
            int currentEpisodeIndex = -1;
            for (int i = 0; i < sortedEpisodes.Count; i++)
            {
                if (sortedEpisodes[i].Id == episode.Id)
                {
                    currentEpisodeIndex = i;
                    break;
                }
            }

            // 获取下一个episode的第一个line
            if (currentEpisodeIndex >= 0 && currentEpisodeIndex < sortedEpisodes.Count - 1)
            {
                var nextEpisode = sortedEpisodes[currentEpisodeIndex + 1];
                if (nextEpisode.HasComponent<Comp.Children>())
                {
                    var nextEpisodeChildrenComp = nextEpisode.GetComponent<Comp.Children>();
                    var sortedLines = GetSortedChildrenByOrder(
                        nextEpisodeChildrenComp.ChildrenEntities
                    );
                    return sortedLines.Count > 0 ? sortedLines[0] : null;
                }
            }

            return null;
        }

        /// <summary>
        /// 按Order组件对子节点排序
        /// </summary>
        private List<Entity> GetSortedChildrenByOrder(List<Entity> children)
        {
            return children
                .Where(child => child.HasComponent<Comp.Order>())
                .OrderBy(child => child.GetComponent<Comp.Order>().Number)
                .ToList();
        }

        #endregion

        #region 增强的跳转功能

        /// <summary>
        /// 执行下一步（支持默认顺序跳转）
        /// </summary>
        public bool Next()
        {
            var nextEntity = GetNextByOrder();
            if (nextEntity != null)
            {
                return MoveToWithHistory(nextEntity);
            }
            return false;
        }

        /// <summary>
        /// 检查当前节点是否是episode的最后一个line
        /// </summary>
        public bool IsLastLineInEpisode()
        {
            if (_currentEntity?.HasComponent<Comp.Parent>() != true)
                return false;

            var parentComp = _currentEntity.GetComponent<Comp.Parent>();
            if (!parentComp.ParentId.HasValue)
                return false;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Comp.Children>() != true)
                return false;

            var childrenComp = parent.GetComponent<Comp.Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);

            if (sortedChildren.Count == 0)
                return false;

            return sortedChildren[sortedChildren.Count - 1].Id == _currentEntity.Id;
        }

        /// <summary>
        /// 获取当前episode的所有可跳转目标（用于最后一个line）
        /// </summary>
        public List<Entity> GetEpisodeJumpTargets()
        {
            var targets = new List<Entity>();

            if (_currentEntity?.HasComponent<Comp.Parent>() != true)
                return targets;

            // 获取当前line的父episode
            var lineParentComp = _currentEntity.GetComponent<Comp.Parent>();
            if (!lineParentComp.ParentId.HasValue)
                return targets;

            var currentEpisode = _ecsFramework.GetEntitySafe(lineParentComp.ParentId.Value);
            if (currentEpisode?.HasComponent<Comp.Parent>() != true)
                return targets;

            // 获取episode的父chapter
            var episodeParentComp = currentEpisode.GetComponent<Comp.Parent>();
            if (!episodeParentComp.ParentId.HasValue)
                return targets;

            var chapter = _ecsFramework.GetEntitySafe(episodeParentComp.ParentId.Value);
            if (chapter?.HasComponent<Comp.Children>() != true)
                return targets;

            var chapterChildrenComp = chapter.GetComponent<Comp.Children>();
            var sortedEpisodes = GetSortedChildrenByOrder(chapterChildrenComp.ChildrenEntities);

            // 添加所有其他episode作为跳转目标
            foreach (var episode in sortedEpisodes)
            {
                if (episode.Id != currentEpisode.Id)
                {
                    targets.Add(episode);
                }
            }

            return targets;
        }

        /// <summary>
        /// 为当前节点（最后一个line）自动配置episode跳转
        /// </summary>
        public void ConfigureEpisodeJump(bool includeCurrentChapterOnly = true)
        {
            if (!IsLastLineInEpisode())
                return;

            // 移除现有的跳转组件（如果有）
            if (_currentEntity.HasComponent<Comp.Jump>())
            {
                _currentEntity.RemoveComponent<Comp.Jump>();
            }

            var jumpComp = Comp.Jump.CreateEpisodeJump();
            var targets = GetEpisodeJumpTargets();

            // 如果限制在当前章节内，需要过滤目标
            if (includeCurrentChapterOnly)
            {
                var currentChapter = GetCurrentChapter();
                if (currentChapter != null)
                {
                    targets = targets
                        .Where(target =>
                        {
                            var targetParent = target.GetComponent<Comp.Parent>();
                            return targetParent?.ParentId == currentChapter.Id;
                        })
                        .ToList();
                }
            }

            foreach (var target in targets)
            {
                jumpComp.AddTarget(target.Id);
            }

            _currentEntity.AddComponent(jumpComp);
        }

        /// <summary>
        /// 获取当前节点所在的chapter
        /// </summary>
        public Entity GetCurrentChapter()
        {
            var entity = _currentEntity;
            while (entity != null)
            {
                if (entity.HasComponent<Comp.Localization>())
                {
                    var loc = entity.GetComponent<Comp.Localization>();
                    if (loc.Type == Comp.Localization.NodeType.Chapter)
                    {
                        return entity;
                    }
                }

                if (!entity.HasComponent<Comp.Parent>())
                    break;

                var parentComp = entity.GetComponent<Comp.Parent>();
                if (!parentComp.ParentId.HasValue)
                    break;

                entity = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            }
            return null;
        }

        #endregion

        #region 增强的Next方法

        /// <summary>
        /// 智能下一步 - 自动处理跳转逻辑
        /// </summary>
        public bool SmartNext()
        {
            if (_currentEntity == null)
                return false;

            // 1. 如果有自定义跳转组件，使用跳转逻辑
            if (_currentEntity.HasComponent<Comp.Jump>())
            {
                var jumpComp = _currentEntity.GetComponent<Comp.Jump>();
                if (jumpComp.Type == Comp.JumpType.Custom && jumpComp.AllowedTargetIds.Count > 0)
                {
                    return JumpTo(jumpComp.AllowedTargetIds[0]);
                }
                // 对于EpisodeTransition类型，需要用户选择，这里使用默认顺序
            }

            // 2. 使用默认顺序跳转
            return Next();
        }

        /// <summary>
        /// 获取所有可能的下一步选项（包括默认顺序和跳转目标）
        /// </summary>
        public List<Entity> GetAllNextOptions()
        {
            var options = new HashSet<Entity>();

            // 添加默认顺序的下一个节点
            var defaultNext = GetNextByOrder();
            if (defaultNext != null)
            {
                options.Add(defaultNext);
            }

            // 添加跳转目标
            if (_currentEntity?.HasComponent<Comp.Jump>() == true)
            {
                var jumpComp = _currentEntity.GetComponent<Comp.Jump>();
                foreach (var targetId in jumpComp.AllowedTargetIds)
                {
                    var target = _ecsFramework.GetEntitySafe(targetId);
                    if (target != null)
                    {
                        options.Add(target);
                    }
                }
            }

            return options.ToList();
        }

        #endregion
    }
}
