using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// �������ָ�룬�ṩ�ڵ㵼�������ɨ�蹦��
    /// </summary>
    public class StoryPointer
    {
        private Entity _currentEntity;
        private readonly ECSFramework _ecsFramework;
        private readonly Stack<Entity> _history = new Stack<Entity>();

        /// <summary>
        /// ��ǰָ���ʵ��
        /// </summary>
        public Entity Current => _currentEntity;

        /// <summary>
        /// ��ǰʵ��ID
        /// </summary>
        public int CurrentId => _currentEntity?.Id ?? -1;

        /// <summary>
        /// �Ƿ�����ʷ��¼�����Է��أ�
        /// </summary>
        public bool HasHistory => _history.Count > 0;

        /// <summary>
        /// �Ƿ�ָ����Чʵ��
        /// </summary>
        public bool IsValid => _currentEntity != null;

        public StoryPointer(ECSFramework ecsFramework, Entity startEntity = null)
        {
            _ecsFramework = ecsFramework ?? throw new ArgumentNullException(nameof(ecsFramework));
            _currentEntity = startEntity;
        }

        /// <summary>
        /// �ƶ���ָ��ʵ��
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
        /// �ƶ���ָ��ʵ�壨����ʷ��¼��
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
        /// ���ص���һ���ڵ�
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
        /// �����ʷ��¼
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// �����ƶ���Ĭ�Ͽ�ʼλ�ã���һ�µ�һС�ڵ�һ�У�
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
        /// ���õ�Ĭ�Ͽ�ʼλ�ã������ʷ��
        /// </summary>
        public void ResetToDefaultStart()
        {
            var defaultStart = StoryTreeManager.Inst.GetDefaultStartPointer();
            if (defaultStart != null && defaultStart.IsValid)
            {
                ClearHistory();
                _currentEntity = defaultStart.Current;
                LogManager.Log($"�����õ�Ĭ�Ͽ�ʼλ��: {GetFormattedPath()}");
            }
        }

        /// <summary>
        /// ��ʼ�µľ���Ự�����õ���ʼλ�ò������ʷ��
        /// </summary>
        public void StartNewSession()
        {
            ResetToDefaultStart();
            ClearHistory();
        }

        #region ��״�ṹ����

        /// <summary>
        /// �ƶ������ڵ�
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
        /// �ƶ�����һ���ӽڵ�
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
        /// �ƶ���ָ���������ӽڵ�
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
        /// �ƶ�����һ���ֵܽڵ�
        /// </summary>
        public bool MoveToNextSibling()
        {
            if (_currentEntity == null)
                return false;

            // ��ȡ���ڵ�
            if (!MoveToParent())
                return false;

            var parent = _currentEntity;
            if (!parent.HasComponent<Comp.Children>())
                return false;

            var childrenComp = parent.GetComponent<Comp.Children>();
            var children = childrenComp.ChildrenEntities;

            // �ҵ���ǰ�ڵ����ֵ��е�λ��
            int currentIndex = -1;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Id == _history.Peek()?.Id) // ��һ���ڵ���ԭ���ĵ�ǰ�ڵ�
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
        /// �ƶ�����һ���ֵܽڵ�
        /// </summary>
        public bool MoveToPreviousSibling()
        {
            if (_currentEntity == null)
                return false;

            // ��ȡ���ڵ�
            if (!MoveToParent())
                return false;

            var parent = _currentEntity;
            if (!parent.HasComponent<Comp.Children>())
                return false;

            var childrenComp = parent.GetComponent<Comp.Children>();
            var children = childrenComp.ChildrenEntities;

            // �ҵ���ǰ�ڵ����ֵ��е�λ��
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

        #region ��ת����

        /// <summary>
        /// ����Ƿ������ת��Ŀ��ڵ�
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
        /// ִ����ת��Ŀ��ڵ�
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
        /// ��ȡ���п���ת��Ŀ��
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

        #region ���ɨ�蹦��

        /// <summary>
        /// ɨ�赱ǰ�ڵ���ض��������
        /// </summary>
        public T ScanComponent<T>()
            where T : IComponent
        {
            return _currentEntity.GetComponent<T>();
        }

        /// <summary>
        /// ɨ�赱ǰ�ڵ��Ƿ�����ض��������
        /// </summary>
        public bool HasComponent<T>()
            where T : IComponent
        {
            return _currentEntity?.HasComponent<T>() == true;
        }

        /// <summary>
        /// ɨ���ӽڵ��е��ض��������
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
        /// ��������ɨ���ض����������������ȣ�
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

            // ɨ�赱ǰ�ڵ�����
            var component = entity.GetComponent<T>();
            if (component != null)
            {
                results.Add(component);
            }

            // �ݹ�ɨ���ӽڵ�
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

        #region ʵ�÷���

        /// <summary>
        /// ��ȡ��ǰ�ڵ�ı��ػ���Ϣ
        /// </summary>
        public Comp.Localization GetLocalizationInfo()
        {
            return ScanComponent<Comp.Localization>();
        }

        /// <summary>
        /// ��ȡ��ǰ�ڵ��˳����Ϣ
        /// </summary>
        public Comp.Order GetOrderInfo()
        {
            return ScanComponent<Comp.Order>();
        }

        /// <summary>
        /// ��ȡ��ʽ��·�����磺��1�� / ��2�� / 3.��
        /// </summary>
        public string GetFormattedPath()
        {
            var path = new List<string>();
            var tempPointer = new StoryPointer(_ecsFramework, _currentEntity);

            // ���ϱ�������·��
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
        /// ����ָ�뵽���ڵ�
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

        #region Ĭ��˳����ת�߼�

        /// <summary>
        /// ��ȡ��ǰ�ڵ����һ���ڵ㣨��Order˳��
        /// </summary>
        public Entity GetNextByOrder()
        {
            if (_currentEntity == null)
                return null;

            // �������ת����Ҳ���Ĭ��˳�����ͣ�����ʹ����ת�߼�
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

            // ���Ի�ȡ��һ���ֵܽڵ�
            var nextSibling = GetNextSiblingByOrder();
            if (nextSibling != null)
                return nextSibling;

            // ���û���ֵܽڵ㣬���Ի�ȡ���ڵ����һ���ֵܽڵ�ĵ�һ���ӽڵ�
            return GetNextEpisodeFirstLine();
        }

        /// <summary>
        /// ��Order˳���ȡ��һ���ֵܽڵ�
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
        /// ��ȡ��һ��episode�ĵ�һ��line�ڵ�
        /// </summary>
        private Entity GetNextEpisodeFirstLine()
        {
            if (_currentEntity?.HasComponent<Comp.Parent>() != true)
                return null;

            // ��ȡ��ǰline�ĸ�episode
            var lineParentComp = _currentEntity.GetComponent<Comp.Parent>();
            if (!lineParentComp.ParentId.HasValue)
                return null;

            var episode = _ecsFramework.GetEntitySafe(lineParentComp.ParentId.Value);
            if (episode?.HasComponent<Comp.Parent>() != true)
                return null;

            // ��ȡepisode�ĸ�chapter
            var episodeParentComp = episode.GetComponent<Comp.Parent>();
            if (!episodeParentComp.ParentId.HasValue)
                return null;

            var chapter = _ecsFramework.GetEntitySafe(episodeParentComp.ParentId.Value);
            if (chapter?.HasComponent<Comp.Children>() != true)
                return null;

            var chapterChildrenComp = chapter.GetComponent<Comp.Children>();
            var sortedEpisodes = GetSortedChildrenByOrder(chapterChildrenComp.ChildrenEntities);

            // �ҵ���ǰepisode��chapter�е�λ��
            int currentEpisodeIndex = -1;
            for (int i = 0; i < sortedEpisodes.Count; i++)
            {
                if (sortedEpisodes[i].Id == episode.Id)
                {
                    currentEpisodeIndex = i;
                    break;
                }
            }

            // ��ȡ��һ��episode�ĵ�һ��line
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
        /// ��Order������ӽڵ�����
        /// </summary>
        private List<Entity> GetSortedChildrenByOrder(List<Entity> children)
        {
            return children
                .Where(child => child.HasComponent<Comp.Order>())
                .OrderBy(child => child.GetComponent<Comp.Order>().Number)
                .ToList();
        }

        #endregion

        #region ��ǿ����ת����

        /// <summary>
        /// ִ����һ����֧��Ĭ��˳����ת��
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
        /// ��鵱ǰ�ڵ��Ƿ���episode�����һ��line
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
        /// ��ȡ��ǰepisode�����п���תĿ�꣨�������һ��line��
        /// </summary>
        public List<Entity> GetEpisodeJumpTargets()
        {
            var targets = new List<Entity>();

            if (_currentEntity?.HasComponent<Comp.Parent>() != true)
                return targets;

            // ��ȡ��ǰline�ĸ�episode
            var lineParentComp = _currentEntity.GetComponent<Comp.Parent>();
            if (!lineParentComp.ParentId.HasValue)
                return targets;

            var currentEpisode = _ecsFramework.GetEntitySafe(lineParentComp.ParentId.Value);
            if (currentEpisode?.HasComponent<Comp.Parent>() != true)
                return targets;

            // ��ȡepisode�ĸ�chapter
            var episodeParentComp = currentEpisode.GetComponent<Comp.Parent>();
            if (!episodeParentComp.ParentId.HasValue)
                return targets;

            var chapter = _ecsFramework.GetEntitySafe(episodeParentComp.ParentId.Value);
            if (chapter?.HasComponent<Comp.Children>() != true)
                return targets;

            var chapterChildrenComp = chapter.GetComponent<Comp.Children>();
            var sortedEpisodes = GetSortedChildrenByOrder(chapterChildrenComp.ChildrenEntities);

            // �����������episode��Ϊ��תĿ��
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
        /// Ϊ��ǰ�ڵ㣨���һ��line���Զ�����episode��ת
        /// </summary>
        public void ConfigureEpisodeJump(bool includeCurrentChapterOnly = true)
        {
            if (!IsLastLineInEpisode())
                return;

            // �Ƴ����е���ת���������У�
            if (_currentEntity.HasComponent<Comp.Jump>())
            {
                _currentEntity.RemoveComponent<Comp.Jump>();
            }

            var jumpComp = Comp.Jump.CreateEpisodeJump();
            var targets = GetEpisodeJumpTargets();

            // ��������ڵ�ǰ�½��ڣ���Ҫ����Ŀ��
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
        /// ��ȡ��ǰ�ڵ����ڵ�chapter
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

        #region ��ǿ��Next����

        /// <summary>
        /// ������һ�� - �Զ�������ת�߼�
        /// </summary>
        public bool SmartNext()
        {
            if (_currentEntity == null)
                return false;

            // 1. ������Զ�����ת�����ʹ����ת�߼�
            if (_currentEntity.HasComponent<Comp.Jump>())
            {
                var jumpComp = _currentEntity.GetComponent<Comp.Jump>();
                if (jumpComp.Type == Comp.JumpType.Custom && jumpComp.AllowedTargetIds.Count > 0)
                {
                    return JumpTo(jumpComp.AllowedTargetIds[0]);
                }
                // ����EpisodeTransition���ͣ���Ҫ�û�ѡ������ʹ��Ĭ��˳��
            }

            // 2. ʹ��Ĭ��˳����ת
            return Next();
        }

        /// <summary>
        /// ��ȡ���п��ܵ���һ��ѡ�����Ĭ��˳�����תĿ�꣩
        /// </summary>
        public List<Entity> GetAllNextOptions()
        {
            var options = new HashSet<Entity>();

            // ���Ĭ��˳�����һ���ڵ�
            var defaultNext = GetNextByOrder();
            if (defaultNext != null)
            {
                options.Add(defaultNext);
            }

            // �����תĿ��
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
