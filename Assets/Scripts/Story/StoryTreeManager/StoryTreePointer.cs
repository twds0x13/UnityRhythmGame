using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;
using static ECS.Comp.Localization;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// 获取实体的显示名称（用于日志）
        /// </summary>
        private string GetEntityDisplayName(Entity entity)
        {
            if (entity == null)
                return "null";

            var order = entity.GetComponent<Order>();
            var localization = entity.GetComponent<Localization>();

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

        public StoryPointer CreatePointer(Entity startEntity = null)
        {
            if (_rootEntity == null)
            {
                _rootEntity = GetOrCreateRoot();
            }

            return new StoryPointer(_ecsFramework, startEntity ?? _rootEntity, true);
        }

        /// <summary>
        /// 获取默认开始位置（第一章第一小节第一行）
        /// </summary>
        /// <returns>指向默认开始位置的指针，如果找不到则返回null</returns>
        public StoryPointer GetDefaultStartPointer()
        {
            try
            {
                var root = GetOrCreateRoot();
                if (root == null || !root.HasComponent<Children>())
                {
                    LogManager.Warning("根节点没有子节点，无法找到默认开始位置");
                    return CreatePointer(root);
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
                return CreatePointer();
            }
        }
    }

    /// <summary>
    /// 剧情遍历指针，提供节点导航和组件扫描功能，支持事件回调
    /// </summary>
    public class StoryPointer
    {
        private Entity _currentEntity;
        private readonly ECSFramework _ecsFramework;
        private readonly Stack<Entity> _history = new Stack<Entity>();

        // 位置信息
        private NodeType _currentNodeType = NodeType.Null;
        private int _chapterNumber = 0;
        private int _episodeNumber = 0;
        private int _lineNumber = 0;

        // 选择系统状态
        private bool _isChoiceBlocked = false;
        private Choice _currentChoice = null;

        // 事件回调系统
        public event Action<NodeType> OnNodeTypeChange; // 暂时没有其他组件使用

        private event Action<int, int, int> _onPositionChange;
        private event Action _onChoiceEncounter;
        private event Action _onChoiceSelect;

        #region 公共属性

        public NodeType NodeType
        {
            get
            {
                var loc = _currentEntity?.GetComponent<Localization>();
                return loc?.Type ?? NodeType.Null;
            }
        }

        /// <summary>
        /// 当前指向的实体
        /// </summary>
        public Entity Current => _currentEntity;

        /// <summary>
        /// 当前实体ID
        /// </summary>
        public int CurrentId => _currentEntity?.Id ?? -1;

        /// <summary>
        /// 当前节点类型
        /// </summary>
        public NodeType CurrentNodeType =>
            _currentEntity != null ? _currentNodeType : NodeType.Null;

        /// <summary>
        /// 当前章节编号
        /// </summary>
        public int ChapterNumber => _currentEntity != null ? _chapterNumber : 0;

        /// <summary>
        /// 当前集数编号
        /// </summary>
        public int EpisodeNumber => _currentEntity != null ? _episodeNumber : 0;

        /// <summary>
        /// 当前行数编号
        /// </summary>
        public int LineNumber => _currentEntity != null ? _lineNumber : 0;

        /// <summary>
        /// 是否指向有效实体
        /// </summary>
        public bool IsValid => _currentEntity != null;

        /// <summary>
        /// 当前是否处于选择阻塞状态
        /// </summary>
        public bool ChoiceBlocked => _isChoiceBlocked;

        /// <summary>
        /// 获取当前的选择组件（如果有）
        /// </summary>
        public List<ChoiceOption> ChoiceOptions => _currentChoice.Options;

        #endregion

        public StoryPointer(
            ECSFramework ecsFramework,
            Entity startEntity = null,
            bool output = false
        )
        {
            _ecsFramework = ecsFramework ?? throw new ArgumentNullException(nameof(ecsFramework));
            _currentEntity = startEntity;

            if (startEntity != null)
            {
                LogManager.Log(
                    $"指针初始化，起始实体ID: {startEntity.Id}",
                    nameof(StoryPointer),
                    output
                );

                UpdatePositionInfoAndNotify(output);
            }
        }

        #region 核心导航方法

        /// <summary>
        /// 智能下一步 - 自动处理跳转逻辑和顺序导航
        /// </summary>
        public bool Next()
        {
            if (_currentEntity == null)
                return false;

            // 如果处于选择阻塞状态，不允许自动前进
            if (_isChoiceBlocked)
            {
                LogManager.Warning("当前处于选择阻塞状态，请先做出选择");
                return false;
            }

            return TryNavigateByOrder();
        }

        /// <summary>
        /// 返回到上一个节点
        /// </summary>
        public bool Back()
        {
            if (_history.Count > 0)
            {
                _currentEntity = _history.Pop();

                ClearChoiceState();
                UpdatePositionInfoAndNotify();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动到指定实体
        /// </summary>
        public bool MoveTo(int entityId, bool output = true)
        {
            var target = _ecsFramework.GetEntitySafe(entityId);
            if (target != null)
            {
                _history.Push(_currentEntity);
                _currentEntity = target;
                ClearChoiceState();
                UpdatePositionInfoAndNotify(output);
                return true;
            }
            return false;
        }

        #endregion

        #region 选择系统

        /// <summary>
        /// 做出选择并跳转到目标节点
        /// </summary>
        /// <param name="option">选择的选项</param>
        /// <returns>是否成功跳转</returns>
        public bool Choice(ChoiceOption option)
        {
            if (option == null)
            {
                LogManager.Error("选择选项不能为null");
                return false;
            }

            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("当前不在选择状态，无法做出选择");
                return false;
            }

            // 验证选项是否有效
            if (!_currentChoice.CanJumpTo(option.TargetId))
            {
                LogManager.Error($"无效的选择选项，目标ID {option.TargetId} 不在选项中");
                return false;
            }

            // 获取目标实体
            var targetEntity = _ecsFramework.GetEntitySafe(option.TargetId);
            if (targetEntity == null)
            {
                LogManager.Error($"选择的目标实体不存在: {option.TargetId}");
                return false;
            }

            LogManager.Log($"做出选择: {option.DisplayText} -> 目标 {option.TargetId}");

            // 触发选择完成事件
            TriggerChoiceSelected();

            // 跳转到目标节点
            bool success = MoveToWithHistory(targetEntity);
            if (success)
            {
                // 清除选择状态
                ClearChoiceState();
            }

            return success;
        }

        /// <summary>
        /// 通过选项索引做出选择
        /// </summary>
        /// <param name="optionIndex">选项索引（从0开始）</param>
        /// <returns>是否成功跳转</returns>
        public bool Choice(int optionIndex)
        {
            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("当前不在选择状态，无法做出选择");
                return false;
            }

            if (optionIndex < 0 || optionIndex >= _currentChoice.Options.Count)
            {
                LogManager.Error(
                    $"选项索引越界: {optionIndex}，有效范围 [0, {_currentChoice.Options.Count - 1}]"
                );
                return false;
            }

            var option = _currentChoice.Options[optionIndex];
            return Choice(option);
        }

        /// <summary>
        /// 通过目标ID做出选择
        /// </summary>
        /// <param name="targetId">目标实体ID</param>
        /// <returns>是否成功跳转</returns>
        public bool ChoiceByTargetId(int targetId)
        {
            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("当前不在选择状态，无法做出选择");
                return false;
            }

            var option = _currentChoice.Options.FirstOrDefault(o => o.TargetId == targetId);
            if (option == null)
            {
                LogManager.Error($"未找到目标ID为 {targetId} 的选项");
                return false;
            }

            return Choice(option);
        }

        /// <summary>
        /// 自动选择第一个选项（用于测试或默认行为）
        /// </summary>
        /// <returns>是否成功跳转</returns>
        public bool AutoChoiceFirst()
        {
            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("当前不在选择状态，无法自动选择");
                return false;
            }

            if (_currentChoice.Options.Count == 0)
            {
                LogManager.Error("没有可用的选项");
                return false;
            }

            LogManager.Log("自动选择第一个选项");
            return Choice(0);
        }

        /// <summary>
        /// 清除选择状态
        /// </summary>
        private void ClearChoiceState()
        {
            _isChoiceBlocked = false;
            _currentChoice = null;
        }

        /// <summary>
        /// 检查并设置选择状态
        /// </summary>
        private void CheckAndSetChoiceState()
        {
            if (_currentEntity?.HasComponent<Choice>() == true)
            {
                var choice = _currentEntity.GetComponent<Choice>();
                if (choice != null && choice.Options.Count > 0)
                {
                    _isChoiceBlocked = true;
                    _currentChoice = choice;
                    LogManager.Log($"进入选择阻塞状态，选项数量: {choice.Options.Count}");

                    // 触发选择遇到事件
                    TriggerChoiceEncountered(choice);
                }
            }
            else
            {
                ClearChoiceState();
            }
        }

        #endregion

        #region 事件管理系统

        /// <summary>
        /// 触发位置变化事件
        /// </summary>
        private void TriggerPositionChanged()
        {
            try
            {
                _onPositionChange?.Invoke(_chapterNumber, _episodeNumber, _lineNumber);
            }
            catch (Exception ex)
            {
                LogManager.Error($"触发位置变化事件时出错: {ex.Message}", nameof(StoryPointer));
            }
        }

        /// <summary>
        /// 触发选择组件事件
        /// </summary>
        private void TriggerChoiceEncountered(Choice choice)
        {
            _onChoiceEncounter?.Invoke();
            LogManager.Info($"检测到选择组件", nameof(StoryPointer));
        }

        /// <summary>
        ///
        /// </summary>
        private void TriggerChoiceSelected()
        {
            _onChoiceSelect?.Invoke();
            LogManager.Info($"选择已完成", nameof(StoryPointer));
        }

        /// <summary>
        /// 触发节点类型变化事件
        /// </summary>
        private void TriggerNodeTypeChanged()
        {
            try
            {
                OnNodeTypeChange?.Invoke(_currentNodeType);
            }
            catch (Exception ex)
            {
                LogManager.Error($"触发节点类型变化事件时出错: {ex.Message}", nameof(StoryPointer));
            }
        }

        /// <summary>
        /// 手动触发当前位置事件（用于外部强制更新）
        /// </summary>
        public void TriggerCurrentEvents()
        {
            UpdatePositionInfoAndNotify();
        }

        /// <summary>
        /// 安全地添加位置变化监听器（避免重复订阅）
        /// </summary>
        public void PositionChange(Action<int, int, int> handler)
        {
            _onPositionChange -= handler;
            _onPositionChange += handler;
        }

        /// <summary>
        /// 安全地添加选择组件监听器（避免重复订阅）
        /// </summary>
        public void ChoiceEncounter(Action handler)
        {
            _onChoiceEncounter -= handler;
            _onChoiceEncounter += handler;
        }

        /// <summary>
        /// 安全地添加选择完成监听器（避免重复订阅）
        /// </summary>
        public void ChoiceSelect(Action handler)
        {
            _onChoiceSelect -= handler;
            _onChoiceSelect += handler;
        }

        /// <summary>
        /// 清除所有事件订阅
        /// </summary>
        public void ClearAllEvents()
        {
            _onPositionChange = null;
            _onChoiceEncounter = null;
            _onChoiceSelect = null;
            OnNodeTypeChange = null;
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
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
                UpdatePositionInfoAndNotify();
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

        /// <summary>
        /// 获取当前节点的顺序信息
        /// </summary>
        public Comp.Order GetOrderInfo()
        {
            return _currentEntity?.GetComponent<Order>();
        }

        /// <summary>
        /// 获取格式化路径（如：第1章 / 第2节 / 3.）
        /// </summary>
        public string GetFormattedPath()
        {
            var path = new List<string>();
            var tempPointer = new StoryPointer(_ecsFramework, _currentEntity);

            while (tempPointer.IsValid)
            {
                var order = tempPointer.GetOrderInfo();
                if (order != null)
                {
                    path.Insert(0, order.FormattedLabel);
                }

                if (!tempPointer.MoveToParent(false))
                    break;
            }

            return string.Join(" / ", path);
        }

        /// <summary>
        /// 检查当前节点是否是episode的最后一个line
        /// </summary>
        public bool IsLastLineInEpisode()
        {
            if (_currentEntity?.HasComponent<Parent>() != true)
                return false;

            var parentComp = _currentEntity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return false;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Children>() != true)
                return false;

            var childrenComp = parent.GetComponent<Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);
            return sortedChildren.Count > 0 && sortedChildren[^1].Id == _currentEntity.Id;
        }

        #endregion

        #region 内部导航实现

        /// <summary>
        /// 按顺序导航到下一个节点
        /// </summary>
        private bool TryNavigateByOrder()
        {
            var nextEntity = GetNextByOrder();
            if (nextEntity != null)
            {
                return MoveToWithHistory(nextEntity);
            }
            return false;
        }

        /// <summary>
        /// 移动到指定实体（带历史记录）
        /// </summary>
        private bool MoveToWithHistory(Entity entity)
        {
            if (entity != null)
            {
                _history.Push(_currentEntity);
                _currentEntity = entity;
                UpdatePositionInfoAndNotify();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动到父节点
        /// </summary>
        private bool MoveToParent(bool output = true)
        {
            if (_currentEntity?.HasComponent<Parent>() == true)
            {
                var parentComp = _currentEntity.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    return MoveTo(parentComp.ParentId.Value, output);
                }
            }
            return false;
        }

        /// <summary>
        /// 获取当前节点的下一个节点（按Order顺序）
        /// </summary>
        private Entity GetNextByOrder()
        {
            if (_currentEntity == null)
                return null;

            // 尝试获取下一个兄弟节点
            var nextSibling = GetNextSiblingByOrder();
            if (nextSibling != null)
                return nextSibling;

            // 在父级层次结构中查找下一个节点
            return GetNextInParentHierarchy();
        }

        /// <summary>
        /// 按Order顺序获取下一个兄弟节点
        /// </summary>
        private Entity GetNextSiblingByOrder()
        {
            if (_currentEntity?.HasComponent<Parent>() != true)
                return null;

            var parentComp = _currentEntity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return null;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Children>();
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
        /// 在父级层次结构中查找下一个节点
        /// </summary>
        private Entity GetNextInParentHierarchy()
        {
            var current = _currentEntity;
            var visited = new HashSet<int> { current.Id };

            while (current != null)
            {
                if (!current.HasComponent<Parent>())
                    break;

                var parentComp = current.GetComponent<Parent>();
                if (!parentComp.ParentId.HasValue)
                    break;

                var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
                if (parent == null)
                    break;

                var nextSibling = GetNextSiblingForEntity(parent);
                if (nextSibling != null && !visited.Contains(nextSibling.Id))
                {
                    return GetFirstLeafNode(nextSibling);
                }

                current = parent;
                visited.Add(current.Id);
            }

            return null;
        }

        /// <summary>
        /// 获取指定实体的下一个兄弟节点
        /// </summary>
        private Entity GetNextSiblingForEntity(Entity entity)
        {
            if (entity?.HasComponent<Parent>() != true)
                return null;

            var parentComp = entity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return null;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);

            int currentIndex = -1;
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                if (sortedChildren[i].Id == entity.Id)
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
        /// 获取指定节点的第一个叶子节点
        /// </summary>
        private Entity GetFirstLeafNode(Entity node)
        {
            if (node == null)
                return null;

            var current = node;
            while (current.HasComponent<Children>())
            {
                var childrenComp = current.GetComponent<Children>();
                var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);
                if (sortedChildren.Count == 0)
                    break;
                current = sortedChildren[0];
            }

            return current;
        }

        /// <summary>
        /// 按Order组件对子节点排序
        /// </summary>
        private List<Entity> GetSortedChildrenByOrder(List<Entity> children)
        {
            return children
                .Where(child => child.HasComponent<Order>())
                .OrderBy(child => child.GetComponent<Order>().Number)
                .ToList();
        }

        #endregion

        #region 位置信息管理

        /// <summary>
        /// 更新位置信息并通知所有事件监听器
        /// </summary>
        private void UpdatePositionInfoAndNotify(bool output = true)
        {
            var oldNodeType = _currentNodeType;
            var oldChapter = _chapterNumber;
            var oldEpisode = _episodeNumber;
            var oldLine = _lineNumber;

            UpdatePositionInfo();

            // 检查是否需要触发事件
            bool positionChanged =
                oldChapter != _chapterNumber
                || oldEpisode != _episodeNumber
                || oldLine != _lineNumber;

            bool nodeTypeChanged = oldNodeType != _currentNodeType;

            if (positionChanged)
            {
                TriggerPositionChanged();
            }

            if (nodeTypeChanged)
            {
                TriggerNodeTypeChanged();
            }

            // 总是检查选择组件
            CheckAndSetChoiceState();

            // 记录调试信息
            if (positionChanged || nodeTypeChanged)
            {
                LogManager.Log(
                    $"指针位置更新: 章节={_chapterNumber}, 集数={_episodeNumber}, 行数={_lineNumber}, 类型={_currentNodeType}",
                    nameof(StoryPointer),
                    output
                );
            }
        }

        /// <summary>
        /// 更新当前节点的位置信息和类型
        /// </summary>
        private void UpdatePositionInfo()
        {
            _currentNodeType = NodeType.Null;

            if (_currentEntity == null)
                return;

            // 通过Localization组件确定节点类型
            if (_currentEntity.HasComponent<Localization>())
            {
                var loc = _currentEntity.GetComponent<Localization>();
                _currentNodeType = loc.Type;
            }

            // 获取章节、集数、行数的编号
            UpdateHierarchyNumbers();
        }

        /// <summary>
        /// 更新层级编号信息
        /// </summary>
        private void UpdateHierarchyNumbers()
        {
            if (_currentEntity == null)
                return;

            var pathFromRoot = _ecsFramework
                .GetFullPathFromRoot(_currentEntity)
                .Where(e => e.Id != 0);

            foreach (var entity in pathFromRoot)
            {
                if (entity.HasComponent<Localization>())
                {
                    var loc = entity.GetComponent<Localization>();

                    switch (loc.Type)
                    {
                        case NodeType.Chapter:
                            _chapterNumber = loc.Number;
                            break;
                        case NodeType.Episode:
                            _episodeNumber = loc.Number;
                            break;
                        case NodeType.Line:
                            _lineNumber = loc.Number;
                            break;
                    }
                }
            }
        }

        #endregion
    }
}
